using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace PerformanceIntelligence.Editor
{
    /// <summary>
    /// Generates a Markdown performance report from a <see cref="CaptureSession"/>,
    /// comparing results against an optional <see cref="PerformanceBudget"/>.
    /// </summary>
    public static class PerformanceReportGenerator
    {
        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Builds a Markdown report string. Pass null for <paramref name="budget"/> to
        /// generate a report without budget comparison (all budget cells show N/A).
        /// </summary>
        public static string GenerateMarkdown(CaptureSession session, PerformanceBudget budget)
        {
            var stats = session.ComputeStats();
            var sc    = session.sceneCensus ?? new SceneCensus();
            var sb    = new StringBuilder();

            // ── Header ─────────────────────────────────────────────────────────
            sb.AppendLine($"# Performance Report — {sc.sceneName}");
            sb.AppendLine();
            sb.AppendLine($"**Platform:** {session.platform}  ");
            sb.AppendLine($"**Captured:** {session.startTimeUtc}  ");
            sb.AppendLine($"**Duration:** {session.durationSeconds:F1}s ({session.frames?.Count ?? 0} frames)  ");
            if (budget != null)
                sb.AppendLine($"**Budget Profile:** {budget.platformName}  ");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            // ── Summary statistics ─────────────────────────────────────────────
            sb.AppendLine("## Summary Statistics");
            sb.AppendLine();
            sb.AppendLine("| Metric | Value |");
            sb.AppendLine("|--------|-------|");
            sb.AppendLine($"| Avg FPS | {stats.avgFPS:F1} |");
            sb.AppendLine($"| Avg Frame Time | {stats.avgFrameMs:F2} ms |");
            sb.AppendLine($"| Worst Frame Time | {stats.worstFrameMs:F2} ms |");
            sb.AppendLine($"| Avg Memory | {stats.avgMemoryMB:F1} MB |");
            sb.AppendLine($"| Peak Memory | {stats.peakMemoryMB:F1} MB |");
            sb.AppendLine($"| Avg GC Alloc/Frame | {stats.avgGCAllocBytes:F0} bytes |");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            // ── Budget comparison ──────────────────────────────────────────────
            sb.AppendLine("## Budget Comparison");
            sb.AppendLine();

            if (budget != null && session.frames != null)
            {
                double avgDrawCalls  = AvgPositive(session.frames, f => f.drawCalls);
                double avgBatches    = AvgPositive(session.frames, f => f.batches);
                double avgTriangles  = AvgPositive(session.frames, f => f.triangles);

                sb.AppendLine("| Metric | Actual | Budget | Status |");
                sb.AppendLine("|--------|--------|--------|--------|");
                sb.AppendLine(BudgetRow("FPS",           stats.avgFPS,           budget.targetFPS,             budget.targetFPS > 0 && stats.avgFPS >= budget.targetFPS));
                sb.AppendLine(BudgetRow("Frame Time (ms)", stats.avgFrameMs,     budget.frameBudgetMs,          stats.avgFrameMs <= budget.frameBudgetMs));
                sb.AppendLine(BudgetRow("Draw Calls",    avgDrawCalls,           budget.maxDrawCalls,           avgDrawCalls < 0 || avgDrawCalls <= budget.maxDrawCalls));
                sb.AppendLine(BudgetRow("Batches",       avgBatches,             budget.maxBatches,             avgBatches    < 0 || avgBatches    <= budget.maxBatches));
                sb.AppendLine(BudgetRow("Triangles",     avgTriangles,           budget.maxTriangles,           avgTriangles  < 0 || avgTriangles  <= budget.maxTriangles));
                sb.AppendLine(BudgetRow("Memory (MB)",   stats.avgMemoryMB,      budget.maxMemoryMB,            stats.avgMemoryMB   <= budget.maxMemoryMB));
                sb.AppendLine(BudgetRow("GC Alloc (bytes/frame)", stats.avgGCAllocBytes, budget.maxGCAllocBytesPerFrame, stats.avgGCAllocBytes <= budget.maxGCAllocBytesPerFrame));
            }
            else
            {
                sb.AppendLine("*No budget profile assigned — skipping comparison.*");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            // ── Scene Census ───────────────────────────────────────────────────
            sb.AppendLine("## Scene Census");
            sb.AppendLine();
            sb.AppendLine("| Object Type | Count |");
            sb.AppendLine("|-------------|-------|");
            sb.AppendLine($"| Active GameObjects | {sc.activeGameObjects} |");
            sb.AppendLine($"| Active Renderers | {sc.activeRenderers} |");
            sb.AppendLine($"| Mesh Renderers | {sc.meshRenderers} |");
            sb.AppendLine($"| Skinned Mesh Renderers | {sc.skinnedMeshRenderers} |");
            sb.AppendLine($"| Particle Systems | {sc.particleSystems} |");
            sb.AppendLine($"| Lights (total) | {sc.lights} |");
            sb.AppendLine($"| Realtime Lights | {sc.realtimeLights} |");
            sb.AppendLine($"| Shadow-Casting Lights | {sc.shadowCastingLights} |");
            sb.AppendLine($"| Cameras | {sc.cameras} |");
            sb.AppendLine($"| Canvases | {sc.canvases} |");
            sb.AppendLine($"| Rigidbodies | {sc.rigidbodies} |");
            sb.AppendLine($"| Colliders | {sc.colliders} |");
            sb.AppendLine($"| Animators | {sc.animators} |");
            sb.AppendLine($"| Unique Materials | {sc.uniqueMaterials} |");
            sb.AppendLine($"| Unique Shaders | {sc.uniqueShaders} |");
            sb.AppendLine($"| Est. Triangle Count | {sc.estimatedTriangleCount:N0} |");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            // ── Risk highlights ────────────────────────────────────────────────
            sb.AppendLine("## Risk Highlights");
            sb.AppendLine();
            var risks = CollectRisks(stats, sc);
            if (risks.Count == 0)
            {
                sb.AppendLine("No significant risks detected.");
            }
            else
            {
                foreach (var r in risks)
                    sb.AppendLine($"- {r}");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine($"*Generated by Performance Intelligence — {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC*");

            return sb.ToString();
        }

        /// <summary>
        /// Writes <paramref name="markdown"/> to
        /// <c>&lt;directory&gt;/report_&lt;sessionId&gt;.md</c>
        /// and calls <see cref="AssetDatabase.Refresh"/>.
        /// </summary>
        public static void SaveReport(string markdown, string directory, string sessionId)
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, $"report_{sessionId}.md");
            File.WriteAllText(path, markdown, Encoding.UTF8);
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"[PerformanceIntelligence] Report saved to {path}");
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private static string BudgetRow(string metric, double actual, double budgetVal, bool pass)
        {
            string actualStr = actual < 0 ? "N/A" : actual.ToString("F1");
            string budgetStr = budgetVal.ToString("F1");
            string status    = actual < 0 ? "N/A" : (pass ? "✓ PASS" : "✗ FAIL");
            return $"| {metric} | {actualStr} | {budgetStr} | {status} |";
        }

        private static double AvgPositive(List<FrameCensus> frames, Func<FrameCensus, long> selector)
        {
            double sum = 0; int count = 0;
            foreach (var f in frames)
            {
                long v = selector(f);
                if (v >= 0) { sum += v; count++; }
            }
            return count > 0 ? sum / count : -1;
        }

        private static List<string> CollectRisks(CaptureStats stats, SceneCensus sc)
        {
            var risks = new List<string>();

            if (sc.realtimeLights > 4)
                risks.Add($"High realtime light count ({sc.realtimeLights}). Consider baked lighting to reduce draw cost.");

            if (sc.shadowCastingLights > 2)
                risks.Add($"Multiple shadow-casting lights ({sc.shadowCastingLights}). Shadow map cost scales per caster.");

            if (sc.skinnedMeshRenderers > 20)
                risks.Add($"High skinned mesh renderer count ({sc.skinnedMeshRenderers}). CPU skinning cost accumulates.");

            if (stats.avgGCAllocBytes > 4096f)
                risks.Add($"Elevated GC allocation per frame ({stats.avgGCAllocBytes:F0} bytes). Check for boxing or per-frame allocations.");

            if (stats.avgFrameMs > 0f && stats.worstFrameMs > stats.avgFrameMs * 2.5f)
                risks.Add($"Frame time spikes detected (worst: {stats.worstFrameMs:F2} ms vs avg: {stats.avgFrameMs:F2} ms).");

            if (sc.estimatedTriangleCount > 500_000)
                risks.Add($"High estimated triangle count ({sc.estimatedTriangleCount:N0}). Consider LOD or occlusion culling.");

            if (sc.uniqueMaterials > 100)
                risks.Add($"High unique material count ({sc.uniqueMaterials}). May prevent GPU instancing and dynamic batching.");

            if (sc.cameras > 2)
                risks.Add($"Multiple active cameras ({sc.cameras}). Each camera adds a full rendering pass.");

            if (stats.peakMemoryMB > 1500f)
                risks.Add($"Peak memory usage ({stats.peakMemoryMB:F0} MB) is high for mobile targets.");

            return risks;
        }
    }
}
