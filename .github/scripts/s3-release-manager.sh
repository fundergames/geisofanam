#!/usr/bin/env bash

set -euo pipefail

ACTION="${1:-}"
TARGET_ENVIRONMENT="${2:-${TARGET_ENVIRONMENT:-}}"
BUILD_ID="${3:-${BUILD_ID:-}}"

AWS_S3_BUCKET="${AWS_S3_BUCKET:-}"
AWS_S3_PREFIX="${AWS_S3_PREFIX:-}"
DEPLOY_PREFIX_OVERRIDE="${DEPLOY_PREFIX_OVERRIDE:-}"
AWS_S3_KEEP_BUILDS="${AWS_S3_KEEP_BUILDS:-10}"
AWS_RELEASE_ENVIRONMENTS="${AWS_RELEASE_ENVIRONMENTS:-dev,staging,production}"
SOURCE_DIR="${SOURCE_DIR:-build/WebGL}"
RELEASE_COPY_TO_CHANNEL="${RELEASE_COPY_TO_CHANNEL:-false}"

if [ -n "$DEPLOY_PREFIX_OVERRIDE" ]; then
  EFFECTIVE_PREFIX="$DEPLOY_PREFIX_OVERRIDE"
else
  EFFECTIVE_PREFIX="$AWS_S3_PREFIX"
fi

EFFECTIVE_PREFIX="${EFFECTIVE_PREFIX#/}"
EFFECTIVE_PREFIX="${EFFECTIVE_PREFIX%/}"
if [ -n "$EFFECTIVE_PREFIX" ]; then
  ROOT_PREFIX="${EFFECTIVE_PREFIX}/"
else
  ROOT_PREFIX=""
fi

BUILDS_ROOT="${ROOT_PREFIX}builds/"
RELEASES_ROOT="${ROOT_PREFIX}releases/"
CHANNELS_ROOT="${ROOT_PREFIX}channels/"

fail() {
  echo "::error::$1" >&2
  exit 1
}

require_aws_bucket() {
  [ -n "$AWS_S3_BUCKET" ] || fail "AWS_S3_BUCKET is required."
}

assert_non_negative_int() {
  local value="$1"
  local label="$2"
  if ! [[ "$value" =~ ^[0-9]+$ ]]; then
    fail "${label} must be a non-negative integer."
  fi
}

as_bool() {
  local value
  value="$(printf "%s" "$1" | tr '[:upper:]' '[:lower:]')"
  case "$value" in
    1|true|yes|y|on) echo "true" ;;
    *) echo "false" ;;
  esac
}

s3_get_if_exists() {
  local key="$1"
  local output_file="$2"
  if aws s3 cp "s3://${AWS_S3_BUCKET}/${key}" "$output_file" >/dev/null 2>&1; then
    return 0
  fi
  return 1
}

json_field_or_empty() {
  local file="$1"
  local field="$2"
  python - "$file" "$field" <<'PY'
import json
import sys

path = sys.argv[1]
field = sys.argv[2]
try:
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)
except Exception:
    print("")
    raise SystemExit(0)

value = data.get(field, "")
if value is None:
    value = ""
print(value)
PY
}

build_exists() {
  local build_id="$1"
  aws s3 ls "s3://${AWS_S3_BUCKET}/${BUILDS_ROOT}${build_id}/" >/dev/null 2>&1
}

publish_build() {
  require_aws_bucket
  [ -n "$BUILD_ID" ] || fail "BUILD_ID is required for publish action."
  [ -d "$SOURCE_DIR" ] || fail "Source directory not found: $SOURCE_DIR"
  assert_non_negative_int "$AWS_S3_KEEP_BUILDS" "AWS_S3_KEEP_BUILDS"

  local build_prefix="${BUILDS_ROOT}${BUILD_ID}/"
  local metadata_file
  metadata_file="$(mktemp)"

  aws s3 sync "$SOURCE_DIR" "s3://${AWS_S3_BUCKET}/${build_prefix}" --delete

  printf '{\n  "buildId": "%s",\n  "buildPrefix": "%s",\n  "commit": "%s",\n  "runId": "%s",\n  "runUrl": "%s/%s/actions/runs/%s",\n  "publishedAt": "%s",\n  "publishedBy": "%s"\n}\n' \
    "$BUILD_ID" \
    "$build_prefix" \
    "${GITHUB_SHA:-}" \
    "${GITHUB_RUN_ID:-}" \
    "${GITHUB_SERVER_URL:-https://github.com}" \
    "${GITHUB_REPOSITORY:-}" \
    "${GITHUB_RUN_ID:-}" \
    "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
    "${GITHUB_ACTOR:-}" > "$metadata_file"
  aws s3 cp "$metadata_file" "s3://${AWS_S3_BUCKET}/${build_prefix}build-metadata.json" >/dev/null
  rm -f "$metadata_file"

  prune_builds

  echo "Published immutable build to s3://${AWS_S3_BUCKET}/${build_prefix}"
  if [ -n "${GITHUB_OUTPUT:-}" ]; then
    {
      echo "build_id=${BUILD_ID}"
      echo "build_prefix=${build_prefix}"
    } >> "$GITHUB_OUTPUT"
  fi
}

collect_protected_build_ids() {
  local protected_ids=()
  IFS=',' read -r -a envs <<< "$AWS_RELEASE_ENVIRONMENTS"
  for env_name in "${envs[@]}"; do
    local env_trimmed
    env_trimmed="$(echo "$env_name" | xargs)"
    [ -n "$env_trimmed" ] || continue
    for slot in current previous; do
      local manifest_key="${RELEASES_ROOT}${env_trimmed}/${slot}.json"
      local manifest_file
      manifest_file="$(mktemp)"
      if s3_get_if_exists "$manifest_key" "$manifest_file"; then
        local protected_id
        protected_id="$(json_field_or_empty "$manifest_file" "buildId")"
        if [ -n "$protected_id" ]; then
          protected_ids+=("$protected_id")
        fi
      fi
      rm -f "$manifest_file"
    done
  done

  if [ "${#protected_ids[@]}" -eq 0 ]; then
    return 0
  fi

  printf "%s\n" "${protected_ids[@]}" | awk 'NF' | sort -u
}

prune_builds() {
  local keep_count="$AWS_S3_KEEP_BUILDS"
  local protected_id
  local -A protected=()

  while IFS= read -r protected_id; do
    [ -n "$protected_id" ] || continue
    protected["$protected_id"]=1
  done < <(collect_protected_build_ids || true)

  local build_prefixes=()
  mapfile -t build_prefixes < <(
    aws s3api list-objects-v2 \
      --bucket "$AWS_S3_BUCKET" \
      --prefix "$BUILDS_ROOT" \
      --delimiter "/" \
      --query 'CommonPrefixes[].Prefix' \
      --output text \
    | tr '\t' '\n' \
    | awk 'NF' \
    | sort
  )

  local remaining="${#build_prefixes[@]}"
  if [ "$remaining" -le "$keep_count" ]; then
    return 0
  fi

  for prefix in "${build_prefixes[@]}"; do
    [ "$remaining" -gt "$keep_count" ] || break
    local candidate_id="${prefix#${BUILDS_ROOT}}"
    candidate_id="${candidate_id%/}"

    if [ -n "${protected[$candidate_id]+x}" ]; then
      continue
    fi

    echo "Pruning old build: ${candidate_id}"
    aws s3 rm "s3://${AWS_S3_BUCKET}/${prefix}" --recursive >/dev/null
    remaining=$((remaining - 1))
  done
}

promote_build() {
  require_aws_bucket
  [ -n "$TARGET_ENVIRONMENT" ] || fail "TARGET_ENVIRONMENT is required for promote action."
  [ -n "$BUILD_ID" ] || fail "BUILD_ID is required for promote action."
  build_exists "$BUILD_ID" || fail "Build not found: ${BUILD_ID}"

  local current_key="${RELEASES_ROOT}${TARGET_ENVIRONMENT}/current.json"
  local previous_key="${RELEASES_ROOT}${TARGET_ENVIRONMENT}/previous.json"
  local history_key="${RELEASES_ROOT}${TARGET_ENVIRONMENT}/history/$(date -u +%Y%m%dT%H%M%SZ)-${BUILD_ID}.json"
  local build_prefix="${BUILDS_ROOT}${BUILD_ID}/"
  local metadata_key="${build_prefix}build-metadata.json"

  local current_file previous_file metadata_file release_file previous_build_id source_commit source_run_url
  current_file="$(mktemp)"
  previous_file="$(mktemp)"
  metadata_file="$(mktemp)"
  release_file="$(mktemp)"
  previous_build_id=""
  source_commit=""
  source_run_url=""

  if s3_get_if_exists "$current_key" "$current_file"; then
    previous_build_id="$(json_field_or_empty "$current_file" "buildId")"
    aws s3 cp "$current_file" "s3://${AWS_S3_BUCKET}/${previous_key}" >/dev/null
  fi

  if s3_get_if_exists "$metadata_key" "$metadata_file"; then
    source_commit="$(json_field_or_empty "$metadata_file" "commit")"
    source_run_url="$(json_field_or_empty "$metadata_file" "runUrl")"
  fi

  printf '{\n  "environment": "%s",\n  "buildId": "%s",\n  "buildPrefix": "%s",\n  "promotedAt": "%s",\n  "promotedBy": "%s",\n  "previousBuildId": "%s",\n  "sourceCommit": "%s",\n  "sourceRunUrl": "%s"\n}\n' \
    "$TARGET_ENVIRONMENT" \
    "$BUILD_ID" \
    "$build_prefix" \
    "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
    "${GITHUB_ACTOR:-}" \
    "$previous_build_id" \
    "$source_commit" \
    "$source_run_url" > "$release_file"

  aws s3 cp "$release_file" "s3://${AWS_S3_BUCKET}/${current_key}" >/dev/null
  aws s3 cp "$release_file" "s3://${AWS_S3_BUCKET}/${history_key}" >/dev/null

  if [ "$(as_bool "$RELEASE_COPY_TO_CHANNEL")" = "true" ]; then
    local channel_prefix="${CHANNELS_ROOT}${TARGET_ENVIRONMENT}/latest/"
    aws s3 sync "s3://${AWS_S3_BUCKET}/${build_prefix}" "s3://${AWS_S3_BUCKET}/${channel_prefix}" --delete >/dev/null
  fi

  echo "Promoted ${BUILD_ID} to ${TARGET_ENVIRONMENT}"
  echo "Release manifest: s3://${AWS_S3_BUCKET}/${current_key}"
  rm -f "$current_file" "$previous_file" "$metadata_file" "$release_file"
}

rollback_build() {
  require_aws_bucket
  [ -n "$TARGET_ENVIRONMENT" ] || fail "TARGET_ENVIRONMENT is required for rollback action."

  local history_prefix="${RELEASES_ROOT}${TARGET_ENVIRONMENT}/history/"
  local history_keys=()
  mapfile -t history_keys < <(
    aws s3api list-objects-v2 \
      --bucket "$AWS_S3_BUCKET" \
      --prefix "$history_prefix" \
      --query 'Contents[].Key' \
      --output text \
    | tr '\t' '\n' \
    | awk 'NF' \
    | sort
  )

  local count="${#history_keys[@]}"
  if [ "$count" -lt 2 ]; then
    fail "Rollback requires at least two releases in ${TARGET_ENVIRONMENT} history."
  fi

  local current_history_key="${history_keys[$((count - 1))]}"
  local rollback_target_key="${history_keys[$((count - 2))]}"
  local target_manifest current_manifest rollback_manifest
  target_manifest="$(mktemp)"
  current_manifest="$(mktemp)"
  rollback_manifest="$(mktemp)"

  aws s3 cp "s3://${AWS_S3_BUCKET}/${rollback_target_key}" "$target_manifest" >/dev/null
  aws s3 cp "s3://${AWS_S3_BUCKET}/${current_history_key}" "$current_manifest" >/dev/null

  local rollback_build_id rollback_from_build_id source_commit source_run_url
  rollback_build_id="$(json_field_or_empty "$target_manifest" "buildId")"
  rollback_from_build_id="$(json_field_or_empty "$current_manifest" "buildId")"
  [ -n "$rollback_build_id" ] || fail "Unable to read buildId from rollback target manifest."

  local build_prefix="${BUILDS_ROOT}${rollback_build_id}/"
  local metadata_key="${build_prefix}build-metadata.json"
  local metadata_file
  metadata_file="$(mktemp)"
  source_commit=""
  source_run_url=""
  if s3_get_if_exists "$metadata_key" "$metadata_file"; then
    source_commit="$(json_field_or_empty "$metadata_file" "commit")"
    source_run_url="$(json_field_or_empty "$metadata_file" "runUrl")"
  fi

  printf '{\n  "environment": "%s",\n  "buildId": "%s",\n  "buildPrefix": "%s",\n  "promotedAt": "%s",\n  "promotedBy": "%s",\n  "previousBuildId": "%s",\n  "sourceCommit": "%s",\n  "sourceRunUrl": "%s",\n  "rollbackFromBuildId": "%s",\n  "rollbackSourceManifest": "%s"\n}\n' \
    "$TARGET_ENVIRONMENT" \
    "$rollback_build_id" \
    "$build_prefix" \
    "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
    "${GITHUB_ACTOR:-}" \
    "$rollback_from_build_id" \
    "$source_commit" \
    "$source_run_url" \
    "$rollback_from_build_id" \
    "$rollback_target_key" > "$rollback_manifest"

  local current_key="${RELEASES_ROOT}${TARGET_ENVIRONMENT}/current.json"
  local previous_key="${RELEASES_ROOT}${TARGET_ENVIRONMENT}/previous.json"
  local rollback_history_key="${history_prefix}$(date -u +%Y%m%dT%H%M%SZ)-rollback-to-${rollback_build_id}.json"

  if s3_get_if_exists "$current_key" "$current_manifest"; then
    aws s3 cp "$current_manifest" "s3://${AWS_S3_BUCKET}/${previous_key}" >/dev/null
  fi

  aws s3 cp "$rollback_manifest" "s3://${AWS_S3_BUCKET}/${current_key}" >/dev/null
  aws s3 cp "$rollback_manifest" "s3://${AWS_S3_BUCKET}/${rollback_history_key}" >/dev/null

  if [ "$(as_bool "$RELEASE_COPY_TO_CHANNEL")" = "true" ]; then
    local channel_prefix="${CHANNELS_ROOT}${TARGET_ENVIRONMENT}/latest/"
    aws s3 sync "s3://${AWS_S3_BUCKET}/${build_prefix}" "s3://${AWS_S3_BUCKET}/${channel_prefix}" --delete >/dev/null
  fi

  echo "Rolled back ${TARGET_ENVIRONMENT} to ${rollback_build_id}"
  echo "Release manifest: s3://${AWS_S3_BUCKET}/${current_key}"
  rm -f "$target_manifest" "$current_manifest" "$rollback_manifest" "$metadata_file"
}

case "$ACTION" in
  publish)
    publish_build
    ;;
  promote)
    promote_build
    ;;
  rollback)
    rollback_build
    ;;
  *)
    echo "Usage:"
    echo "  s3-release-manager.sh publish"
    echo "  s3-release-manager.sh promote <environment> <build_id>"
    echo "  s3-release-manager.sh rollback <environment>"
    fail "Unknown action: ${ACTION}"
    ;;
esac
