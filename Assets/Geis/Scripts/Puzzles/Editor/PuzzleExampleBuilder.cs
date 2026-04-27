/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

using Geis.Puzzles;
using Geis.SoulRealm.WeaponAbilities;
using UnityEditor;
using UnityEngine;

namespace Geis.Puzzles.Editor
{
    /// <summary>
    /// Editor tool that builds self-contained example groups in the open scene.
    /// Use <c>Tools → Puzzles → Examples → …</c> to add one type (including Phase Shift Object), or <c>Create All Examples</c> for the full grid.
    /// Singles are parented under <c>PuzzleExamples</c> (created if missing) and placed on the next free grid slot.
    /// </summary>
    public static class PuzzleExampleBuilder
    {
        private const float ColSpacing = 14f;
        private const float RowSpacing = 16f;

        private const string ExamplesMenu = "Tools/Puzzles/Examples/";

        private const string PhaseShiftDissolveMaterialPath =
            "Assets/Geis/Materials/GeisPhaseShiftDissolve_Default.mat";

        [MenuItem(ExamplesMenu + "Create All Examples", false, -100)]
        public static void CreateAllExamples()
        {
            var root = new GameObject("PuzzleExamples");
            Undo.RegisterCreatedObjectUndo(root, "Create Puzzle Examples");

            // Row 0 — weapon-specific triggers
            CreateSwordBreak     (root, Col(0), Row(0));
            CreateSoulPulse      (root, Col(1), Row(0));
            CreateBowTarget      (root, Col(2), Row(0));
            CreateBowMarkShoot   (root, Col(3), Row(0));
            CreateDaggerSocket   (root, Col(4), Row(0));

            // Row 1 — core interaction triggers
            CreatePressurePlate  (root, Col(0), Row(1));
            CreateSequence       (root, Col(1), Row(1));
            CreateAlignmentDial  (root, Col(2), Row(1));
            CreateDualRealm      (root, Col(3), Row(1));
            CreateEchoImprint    (root, Col(4), Row(1));

            // Row 2 — dagger soul blink + socket, phase shift prop
            CreateDaggerBlinkSocket(root, Col(0), Row(2));
            CreatePhaseShiftObject(root, Col(1), Row(2));

            Selection.activeGameObject = root;
            SceneView.FrameLastActiveSceneView();
        }

        [MenuItem(ExamplesMenu + "Alignment Dial", false, 10)]
        static void MenuCreateAlignmentDial() => CreateSingleExample(CreateAlignmentDial);

        [MenuItem(ExamplesMenu + "Bow Mark + Shoot", false, 11)]
        static void MenuCreateBowMarkShoot() => CreateSingleExample(CreateBowMarkShoot);

        [MenuItem(ExamplesMenu + "Bow Target", false, 12)]
        static void MenuCreateBowTarget() => CreateSingleExample(CreateBowTarget);

        [MenuItem(ExamplesMenu + "Dagger Blink Socket", false, 13)]
        static void MenuCreateDaggerBlinkSocket() => CreateSingleExample(CreateDaggerBlinkSocket);

        [MenuItem(ExamplesMenu + "Phase Shift Object", false, 14)]
        static void MenuCreatePhaseShiftObject() => CreateSingleExample(CreatePhaseShiftObject);

        [MenuItem(ExamplesMenu + "Dagger Socket", false, 15)]
        static void MenuCreateDaggerSocket() => CreateSingleExample(CreateDaggerSocket);

        [MenuItem(ExamplesMenu + "Dual Realm", false, 16)]
        static void MenuCreateDualRealm() => CreateSingleExample(CreateDualRealm);

        [MenuItem(ExamplesMenu + "Echo Imprint", false, 17)]
        static void MenuCreateEchoImprint() => CreateSingleExample(CreateEchoImprint);

        [MenuItem(ExamplesMenu + "Pressure Plate", false, 18)]
        static void MenuCreatePressurePlate() => CreateSingleExample(CreatePressurePlate);

        [MenuItem(ExamplesMenu + "Sequence", false, 19)]
        static void MenuCreateSequence() => CreateSingleExample(CreateSequence);

        [MenuItem(ExamplesMenu + "Soul Pulse", false, 20)]
        static void MenuCreateSoulPulse() => CreateSingleExample(CreateSoulPulse);

        [MenuItem(ExamplesMenu + "Sword Break", false, 21)]
        static void MenuCreateSwordBreak() => CreateSingleExample(CreateSwordBreak);

        static void CreateSingleExample(System.Action<GameObject, float, float> build)
        {
            GameObject root = EnsurePuzzleExamplesRoot();
            NextGridSlot(root.transform, out float x, out float z);
            build(root, x, z);
            Selection.activeGameObject = root;
            SceneView.FrameLastActiveSceneView();
        }

        static GameObject EnsurePuzzleExamplesRoot()
        {
            var go = GameObject.Find("PuzzleExamples");
            if (go != null)
                return go;

            go = new GameObject("PuzzleExamples");
            Undo.RegisterCreatedObjectUndo(go, "Create Puzzle Example");
            return go;
        }

        /// <summary>Places the next single example in row-major order on the same grid as <see cref="CreateAllExamples"/>.</summary>
        static void NextGridSlot(Transform parent, out float x, out float z)
        {
            int i = parent.childCount;
            const int columns = 5;
            x = Col(i % columns);
            z = Row(i / columns);
        }

        // ── Row 0 ────────────────────────────────────────────────────────────────

        static void CreateSwordBreak(GameObject root, float x, float z)
        {
            var parent = CreateGroup(root, "Example_SwordBreak [hit to open]", x, z);

            // Trigger zone
            var zone = CreateBox(parent, "SwordZone", new Vector3(0, 0.05f, 0),
                new Vector3(2f, 0.1f, 2f), PuzzleRealmColors.PhysicalOnly);
            var trigger = zone.AddComponent<SwordHitTrigger>();
            SetInt(trigger, "hitsRequired", 1);
            SetEnum(trigger, "realmMode", (int)PuzzleRealmMode.PhysicalOnly);

            // Output door
            var door = CreateBox(parent, "Door", new Vector3(0, 1.5f, 4f),
                new Vector3(1.5f, 3f, 0.2f), new Color(0.45f, 0.38f, 0.32f));
            var output = door.AddComponent<DoorOutput>();
            SetVector3(output, "openPositionOffset", new Vector3(0f, 3.5f, 0f));

            WireGroup(parent, new Component[] { trigger }, new Component[] { output });
            CreateWorldLabel(parent, "SWORD BREAK\nHit zone to open door\n[Physical Realm]",
                new Vector3(0f, 4f, 0f), PuzzleRealmColors.PhysicalOnly);
        }

        static void CreateSoulPulse(GameObject root, float x, float z)
        {
            var parent = CreateGroup(root, "Example_SoulPulse [pulse to dissolve barrier]", x, z);

            var node = CreateSphere(parent, "PulseNode", new Vector3(0, 0.5f, 0),
                0.5f, PuzzleRealmColors.SoulOnly);
            var trigger = node.AddComponent<SoulPulseReceptorTrigger>();
            SetFloat(trigger, "detectionRadius", 2f);
            SetEnum(trigger, "realmMode", (int)PuzzleRealmMode.SoulOnly);

            var barrier = CreateBox(parent, "Barrier", new Vector3(0, 1.5f, 4f),
                new Vector3(2f, 3f, 0.25f),
                new Color(PuzzleRealmColors.SoulOnly.r, PuzzleRealmColors.SoulOnly.g, PuzzleRealmColors.SoulOnly.b, 0.6f));
            var output = barrier.AddComponent<BarrierOutput>();
            SetComponent(output, "barrierRenderer", barrier.GetComponent<Renderer>());
            SetComponent(output, "barrierCollider", barrier.GetComponent<Collider>());

            WireGroup(parent, new Component[] { trigger }, new Component[] { output });
            CreateWorldLabel(parent, "SOUL PULSE\nPulse node to dissolve barrier\n[Soul Realm]",
                new Vector3(0f, 4f, 0f), PuzzleRealmColors.SoulOnly);
        }

        static void CreateBowTarget(GameObject root, float x, float z)
        {
            var parent = CreateGroup(root, "Example_BowTarget [shoot to raise block]", x, z);

            var target = CreateSphere(parent, "Target", new Vector3(0, 1f, 5f),
                0.5f, PuzzleRealmColors.PhysicalOnly);
            var col = target.GetComponent<SphereCollider>();
            if (col == null) col = target.AddComponent<SphereCollider>();
            col.isTrigger = true;
            var trigger = target.AddComponent<BowTargetTrigger>();
            SetFloat(trigger, "detectionRadius", 0.9f);
            SetEnum(trigger, "realmMode", (int)PuzzleRealmMode.PhysicalOnly);

            var block = CreateBox(parent, "Block", new Vector3(0, 0.5f, 0),
                new Vector3(1.5f, 1f, 1.5f), new Color(0.5f, 0.5f, 0.5f));
            var output = block.AddComponent<RaiseLowerBlockOutput>();
            SetComponent(output, "block", block.transform);
            SetVector3(output, "raisedOffset", new Vector3(0f, 3f, 0f));

            WireGroup(parent, new Component[] { trigger }, new Component[] { output });
            CreateWorldLabel(parent, "BOW TARGET\nShoot sphere to raise block\n[Physical Realm]",
                new Vector3(0f, 4f, 0f), PuzzleRealmColors.PhysicalOnly);
        }

        static void CreateBowMarkShoot(GameObject root, float x, float z)
        {
            var parent = CreateGroup(root, "Example_BowMark [mark soul → shoot physical]", x, z);

            var node = CreateSphere(parent, "MarkNode", new Vector3(0, 1.2f, 4f),
                0.45f, PuzzleRealmColors.BothRealms);
            var col = node.GetComponent<SphereCollider>();
            if (col == null) col = node.AddComponent<SphereCollider>();
            col.isTrigger = true;
            var trigger = node.AddComponent<BowMarkTargetTrigger>();
            SetFloat(trigger, "markRange", 4f);
            SetFloat(trigger, "markDuration", 12f);
            SetEnum(trigger, "realmMode", (int)PuzzleRealmMode.BothRealms);

            var door = CreateBox(parent, "Door", new Vector3(0, 1.5f, -3f),
                new Vector3(1.5f, 3f, 0.2f), PuzzleRealmColors.BothRealms);
            var output = door.AddComponent<DoorOutput>();
            SetVector3(output, "openPositionOffset", new Vector3(0f, 3.5f, 0f));

            WireGroup(parent, new Component[] { trigger }, new Component[] { output });
            CreateWorldLabel(parent, "BOW MARK + SHOOT\nMark in soul realm, shoot in physical\n[Both Realms]",
                new Vector3(0f, 4f, 0f), PuzzleRealmColors.BothRealms);
        }

        static void CreateDaggerSocket(GameObject root, float x, float z)
        {
            var parent = CreateGroup(root, "Example_DaggerSocket [place object to raise block]", x, z);

            var socket = CreateBox(parent, "Socket", new Vector3(0, 0.05f, 0),
                new Vector3(1.5f, 0.1f, 1.5f), PuzzleRealmColors.PhysicalOnly);
            var col = socket.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            var trigger = socket.AddComponent<DaggerSocketTrigger>();
            SetString(trigger, "acceptedTag", "DaggerMovable");
            SetEnum(trigger, "realmMode", (int)PuzzleRealmMode.PhysicalOnly);

            // A movable placeholder the designer can tag "DaggerMovable"
            var movable = CreateBox(parent, "MovableObject [tag: DaggerMovable]",
                new Vector3(4f, 0.5f, 0f), new Vector3(1f, 1f, 1f),
                new Color(0.95f, 0.55f, 0.28f));

            var block = CreateBox(parent, "Block", new Vector3(0, 0.5f, -4f),
                new Vector3(1.5f, 1f, 1.5f), new Color(0.5f, 0.5f, 0.5f));
            var output = block.AddComponent<RaiseLowerBlockOutput>();
            SetComponent(output, "block", block.transform);
            SetVector3(output, "raisedOffset", new Vector3(0f, 3f, 0f));

            WireGroup(parent, new Component[] { trigger }, new Component[] { output });
            CreateWorldLabel(parent, "DAGGER SOCKET\nTag object 'DaggerMovable', place in socket\n[Physical Realm]",
                new Vector3(0f, 4f, 0f), PuzzleRealmColors.PhysicalOnly);
        }

        /// <summary>
        /// Soul Realm Object Blink: <see cref="SoulBlinkable"/> + <see cref="SoulBlinkSocket"/> on the floor socket.
        /// Q targets the socket or the cube; cube needs tag <c>DaggerMovable</c> for <see cref="DaggerSocketTrigger"/>.
        /// </summary>
        static void CreateDaggerBlinkSocket(GameObject root, float x, float z)
        {
            var parent = CreateGroup(root, "Example_DaggerBlinkSocket [Soul Realm Q → blink into socket]", x, z);

            var socket = CreateBox(parent, "Socket", new Vector3(0, 0.05f, 0),
                new Vector3(1.5f, 0.1f, 1.5f), PuzzleRealmColors.SoulOnly);
            var sockCol = socket.GetComponent<Collider>();
            if (sockCol != null)
                sockCol.isTrigger = true;

            var snapAnchor = new GameObject("SnapAnchor");
            snapAnchor.transform.SetParent(socket.transform, false);
            snapAnchor.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            Undo.RegisterCreatedObjectUndo(snapAnchor, "Create Puzzle Examples");

            var daggerTrig = socket.AddComponent<DaggerSocketTrigger>();
            SetString(daggerTrig, "acceptedTag", "DaggerMovable");
            SetEnum(daggerTrig, "realmMode", (int)PuzzleRealmMode.BothRealms);

            var movable = CreateBox(parent, "MovableObject [SoulBlinkable + tag DaggerMovable]",
                new Vector3(4f, 0.5f, 0f), new Vector3(1f, 1f, 1f),
                new Color(0.95f, 0.55f, 0.28f));

            var blink = movable.AddComponent<SoulBlinkable>();
            var blinkSocket = socket.AddComponent<SoulBlinkSocket>();
            SetComponent(blinkSocket, "blinkTarget", blink);
            SetComponent(blinkSocket, "snapAnchor", snapAnchor.transform);
            SetComponent(blink, "socketB", blinkSocket);

            var block = CreateBox(parent, "Block", new Vector3(0, 0.5f, -4f),
                new Vector3(1.5f, 1f, 1.5f), new Color(0.5f, 0.5f, 0.5f));
            var output = block.AddComponent<RaiseLowerBlockOutput>();
            SetComponent(output, "block", block.transform);
            SetVector3(output, "raisedOffset", new Vector3(0f, 3f, 0f));

            WireGroup(parent, new Component[] { daggerTrig }, new Component[] { output });
            CreateWorldLabel(parent,
                "BLINK SOCKET\nSoul Realm Q on socket or cube • tag 'DaggerMovable'\n[Both Realms trigger]",
                new Vector3(0f, 4f, 0f), PuzzleRealmColors.SoulOnly);
        }

        /// <summary>
        /// Dagger secondary: <see cref="SoulPhaseShiftable"/> + <see cref="SoulPhaseShiftPresentation"/> —
        /// Soul Realm F tap starts timed phase-through; physical realm hold F pulls the prop solid.
        /// </summary>
        static void CreatePhaseShiftObject(GameObject root, float x, float z)
        {
            var parent = CreateGroup(root, "Example_PhaseShift [dagger F soul/physical]", x, z);

            var cube = CreateBox(parent, "PhaseShiftBlock",
                new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f),
                new Color(0.42f, 0.78f, 0.98f));

            var presentation = cube.AddComponent<SoulPhaseShiftPresentation>();
            var shift = cube.AddComponent<SoulPhaseShiftable>();
            SetComponent(shift, "presentation", presentation);

            var rend = cube.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.SetPropertyBlock(null);
                Material dissolveMat = AssetDatabase.LoadAssetAtPath<Material>(PhaseShiftDissolveMaterialPath);
                if (dissolveMat != null)
                    rend.sharedMaterial = dissolveMat;
            }

            CreateWorldLabel(parent,
                "PHASE SHIFT\nSoul: F tap • Physical: hold F → solidify\nMat: Geis/SoulRealm/PhaseShiftDissolve",
                new Vector3(0f, 3.5f, 0f), PuzzleRealmColors.BothRealms);
        }

        // ── Row 1 ────────────────────────────────────────────────────────────────

        static void CreatePressurePlate(GameObject root, float x, float z)
        {
            var parent = CreateGroup(root, "Example_PressurePlate [stand to move platform]", x, z);

            var plate = CreateBox(parent, "Plate", new Vector3(0, 0.05f, 0),
                new Vector3(2f, 0.1f, 2f), PuzzleRealmColors.BothRealms);
            var col = plate.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            var trigger = plate.AddComponent<PressurePlateTrigger>();
            SetString(trigger, "activatorTag", "Player");
            SetComponent(trigger, "plateVisual", plate.transform);
            SetEnum(trigger, "realmMode", (int)PuzzleRealmMode.BothRealms);

            // Moving platform with two waypoints
            var platform = CreateBox(parent, "Platform", new Vector3(0, 0.5f, 5f),
                new Vector3(2.5f, 0.25f, 2.5f), new Color(0.6f, 0.6f, 0.9f));
            var wp0 = new GameObject("Waypoint0");
            wp0.transform.SetParent(parent.transform, false);
            wp0.transform.localPosition = new Vector3(0f, 0.5f, 5f);
            var wp1 = new GameObject("Waypoint1");
            wp1.transform.SetParent(parent.transform, false);
            wp1.transform.localPosition = new Vector3(0f, 0.5f, 10f);
            Undo.RegisterCreatedObjectUndo(wp0, "Create Puzzle Examples");
            Undo.RegisterCreatedObjectUndo(wp1, "Create Puzzle Examples");

            var mover = platform.AddComponent<PlatformMover>();
            SetObjectArray(mover, "waypoints", new Object[] { wp0.transform, wp1.transform });

            var movOutput = platform.AddComponent<MovingPlatformOutput>();
            SetComponent(movOutput, "platformMover", mover);

            WireGroup(parent, new Component[] { trigger }, new Component[] { movOutput }, oneShot: false);
            CreateWorldLabel(parent, "PRESSURE PLATE\nStand on plate to move platform\n[Both Realms]",
                new Vector3(0f, 4f, 0f), PuzzleRealmColors.BothRealms);
        }

        static void CreateSequence(GameObject root, float x, float z)
        {
            var parent = CreateGroup(root, "Example_Sequence [hit 3 in order]", x, z);

            // Three step switches at different positions
            var steps = new SoulSwitchTrigger[3];
            var soul = PuzzleRealmColors.SoulOnly;
            var stepColors = new Color[]
            {
                soul,
                new Color(Mathf.Min(1f, soul.r + 0.08f), Mathf.Min(1f, soul.g + 0.1f), soul.b),
                new Color(Mathf.Min(1f, soul.r + 0.16f), Mathf.Min(1f, soul.g + 0.2f), Mathf.Min(1f, soul.b + 0.05f)),
            };
            for (int i = 0; i < 3; i++)
            {
                float angle = -30f + i * 30f;
                float rad   = Mathf.Deg2Rad * angle;
                var pos = new Vector3(Mathf.Sin(rad) * 2.5f, 1f, Mathf.Cos(rad) * 2.5f);
                var stepGo = CreateBox(parent, $"Step_{i + 1}", pos,
                    new Vector3(0.8f, 0.8f, 0.8f), stepColors[i]);
                var stepCol = stepGo.GetComponent<Collider>();
                if (stepCol != null) stepCol.isTrigger = true;
                var sw = stepGo.AddComponent<SoulSwitchTrigger>();
                SetEnum(sw, "realmMode", (int)PuzzleRealmMode.SoulOnly);
                steps[i] = sw;

                // Numbered order label above each step
                CreateWorldLabel(stepGo, $"{i + 1}", new Vector3(0f, 1.5f, 0f), stepColors[i], 24);
            }

            // Sequence trigger holder (invisible)
            var seqGo = new GameObject("SequenceTrigger");
            seqGo.transform.SetParent(parent.transform, false);
            seqGo.transform.localPosition = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(seqGo, "Create Puzzle Examples");
            var seq = seqGo.AddComponent<SequenceTrigger>();
            SetObjectArray(seq, "steps", new Object[] { steps[0], steps[1], steps[2] });
            SetEnum(seq, "realmMode", (int)PuzzleRealmMode.SoulOnly);

            var door = CreateBox(parent, "Door", new Vector3(0, 1.5f, 5f),
                new Vector3(1.5f, 3f, 0.2f), PuzzleRealmColors.SoulOnly);
            var output = door.AddComponent<DoorOutput>();
            SetVector3(output, "openPositionOffset", new Vector3(0f, 3.5f, 0f));

            WireGroup(parent, new Component[] { seq }, new Component[] { output });
            CreateWorldLabel(parent, "SEQUENCE\nActivate steps 1→2→3 in order\n[Soul Realm]",
                new Vector3(0f, 4f, 0f), PuzzleRealmColors.SoulOnly);
        }

        static void CreateAlignmentDial(GameObject root, float x, float z)
        {
            var parent = CreateGroup(root, "Example_AlignmentDial [rotate to 90° to open]", x, z);

            var dialBase = CreateBox(parent, "DialBase", new Vector3(0, 0.5f, 0),
                new Vector3(1f, 1f, 1f), new Color(0.5f, 0.5f, 0.5f));
            var dialVisual = CreateBox(parent, "DialArrow", new Vector3(0, 1.1f, 0),
                new Vector3(0.15f, 0.15f, 0.9f), PuzzleRealmColors.SoulOnly);
            dialVisual.transform.SetParent(dialBase.transform, true);
            var col = dialBase.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            var trigger = dialBase.AddComponent<AlignmentDialTrigger>();
            SetFloat(trigger, "targetAngle", 90f);
            SetFloat(trigger, "snapThreshold", 15f);
            SetFloat(trigger, "rotationSpeed", 90f);
            SetComponent(trigger, "dialVisual", dialVisual.transform);
            SetEnum(trigger, "realmMode", (int)PuzzleRealmMode.SoulOnly);

            var barrier = CreateBox(parent, "Barrier", new Vector3(0, 1.5f, 4f),
                new Vector3(2f, 3f, 0.2f),
                new Color(PuzzleRealmColors.SoulOnly.r, PuzzleRealmColors.SoulOnly.g, PuzzleRealmColors.SoulOnly.b, 0.7f));
            var output = barrier.AddComponent<BarrierOutput>();
            SetComponent(output, "barrierRenderer", barrier.GetComponent<Renderer>());
            SetComponent(output, "barrierCollider", barrier.GetComponent<Collider>());

            WireGroup(parent, new Component[] { trigger }, new Component[] { output });
            CreateWorldLabel(parent, "ALIGNMENT DIAL\nHold E + move to rotate to 90°\n[Soul Realm]",
                new Vector3(0f, 4f, 0f), PuzzleRealmColors.SoulOnly);
        }

        static void CreateDualRealm(GameObject root, float x, float z)
        {
            var parent = CreateGroup(root, "Example_DualRealm [both realms active → open]", x, z);

            // Soul sub-trigger
            var soulGo = CreateBox(parent, "SoulSwitch [soul realm]",
                new Vector3(-2f, 1f, 1f), new Vector3(0.8f, 0.8f, 0.8f), PuzzleRealmColors.SoulOnly);
            var soulCol = soulGo.GetComponent<Collider>();
            if (soulCol != null) soulCol.isTrigger = true;
            var soulSw = soulGo.AddComponent<SoulSwitchTrigger>();
            SetEnum(soulSw, "realmMode", (int)PuzzleRealmMode.SoulOnly);

            // Physical sub-trigger (plate)
            var physGo = CreateBox(parent, "PhysPlate [physical realm]",
                new Vector3(2f, 0.05f, 1f), new Vector3(1.5f, 0.1f, 1.5f), PuzzleRealmColors.PhysicalOnly);
            var physCol = physGo.GetComponent<Collider>();
            if (physCol != null) physCol.isTrigger = true;
            var physPlate = physGo.AddComponent<PressurePlateTrigger>();
            SetEnum(physPlate, "realmMode", (int)PuzzleRealmMode.PhysicalOnly);

            // Dual-realm composite (invisible holder)
            var dualGo = new GameObject("DualRealmTrigger");
            dualGo.transform.SetParent(parent.transform, false);
            dualGo.transform.localPosition = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(dualGo, "Create Puzzle Examples");
            var dual = dualGo.AddComponent<DualRealmTrigger>();
            SetComponent(dual, "soulTrigger", soulSw);
            SetComponent(dual, "physicalTrigger", physPlate);
            SetEnum(dual, "realmMode", (int)PuzzleRealmMode.BothRealms);

            var door = CreateBox(parent, "Door", new Vector3(0, 1.5f, 5f),
                new Vector3(1.5f, 3f, 0.2f), PuzzleRealmColors.BothRealms);
            var output = door.AddComponent<DoorOutput>();
            SetVector3(output, "openPositionOffset", new Vector3(0f, 3.5f, 0f));

            WireGroup(parent, new Component[] { dual }, new Component[] { output });
            CreateWorldLabel(parent, "DUAL REALM\nActivate soul switch AND stand on plate\n[Both Realms]",
                new Vector3(0f, 4f, 0f), PuzzleRealmColors.BothRealms);
        }

        static void CreateEchoImprint(GameObject root, float x, float z)
        {
            var parent = CreateGroup(root, "Example_EchoImprint [leave echo to open door]", x, z);

            // The echo trigger zone (where player presses F)
            var echoZone = CreateBox(parent, "EchoZone [press F here in soul realm]",
                new Vector3(0f, 0.05f, 0f), new Vector3(2f, 0.1f, 2f), PuzzleRealmColors.SoulOnly);
            var echoCol = echoZone.GetComponent<Collider>();
            if (echoCol != null) echoCol.isTrigger = true;
            var echo = echoZone.AddComponent<EchoImprintTrigger>();
            SetFloat(echo, "echoDuration", 8f);
            SetEnum(echo, "realmMode", (int)PuzzleRealmMode.SoulOnly);

            var door = CreateBox(parent, "Door", new Vector3(0, 1.5f, 5f),
                new Vector3(1.5f, 3f, 0.2f), PuzzleRealmColors.SoulOnly);
            var output = door.AddComponent<DoorOutput>();
            SetVector3(output, "openPositionOffset", new Vector3(0f, 3.5f, 0f));

            WireGroup(parent, new Component[] { echo }, new Component[] { output });
            CreateWorldLabel(parent, "ECHO IMPRINT\nPress F in soul realm to leave echo\n[Soul Realm]",
                new Vector3(0f, 4f, 0f), PuzzleRealmColors.SoulOnly);
        }

        // ── Wiring ───────────────────────────────────────────────────────────────

        static void WireGroup(GameObject parent, Component[] triggers, Component[] outputs, bool oneShot = true)
        {
            var group = parent.AddComponent<PuzzleGroup>();
            var so    = new SerializedObject(group);
            so.Update();

            var trigProp = so.FindProperty("triggers");
            trigProp.arraySize = triggers.Length;
            for (int i = 0; i < triggers.Length; i++)
                trigProp.GetArrayElementAtIndex(i).objectReferenceValue = triggers[i];

            var outProp = so.FindProperty("outputs");
            outProp.arraySize = outputs.Length;
            for (int i = 0; i < outputs.Length; i++)
                outProp.GetArrayElementAtIndex(i).objectReferenceValue = outputs[i];

            so.FindProperty("oneShot").boolValue = oneShot;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Primitive helpers ─────────────────────────────────────────────────────

        static GameObject CreateGroup(GameObject root, string name, float x, float z)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3(x, 0f, z);
            Undo.RegisterCreatedObjectUndo(go, "Create Puzzle Examples");
            return go;
        }

        static GameObject CreateBox(GameObject parent, string name, Vector3 localPos,
            Vector3 scale, Color col)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = scale;
            ApplyColour(go, col);
            Undo.RegisterCreatedObjectUndo(go, "Create Puzzle Examples");
            return go;
        }

        static GameObject CreateSphere(GameObject parent, string name, Vector3 localPos,
            float radius, Color col)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = Vector3.one * radius * 2f;
            ApplyColour(go, col);
            Undo.RegisterCreatedObjectUndo(go, "Create Puzzle Examples");
            return go;
        }

        static void ApplyColour(GameObject go, Color col)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null) return;
            // Use a MaterialPropertyBlock so we don't create per-object material assets
            var mpb = new MaterialPropertyBlock();
            rend.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", col);
            mpb.SetColor("_Color",     col);
            rend.SetPropertyBlock(mpb);
        }

        static float Col(int c) => c * ColSpacing;
        static float Row(int r) => r * RowSpacing;

        // ── World-space text labels (visible during Play mode) ────────────────────

        /// <summary>
        /// Creates a child GameObject with a <see cref="TextMesh"/> so the label is visible
        /// in the 3D world during Play mode (no TMP required).
        /// </summary>
        static void CreateWorldLabel(GameObject parent, string text, Vector3 localPos,
            Color col, int fontSize = 14)
        {
            var go = new GameObject("Label_" + text.Split('\n')[0]);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            Undo.RegisterCreatedObjectUndo(go, "Create Puzzle Examples");

            var tm = go.AddComponent<TextMesh>();
            tm.text      = text;
            tm.fontSize  = fontSize;
            tm.color     = col;
            tm.anchor    = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = 0.12f;
        }

        // ── SerializedObject field helpers ────────────────────────────────────────

        static void SetInt(Component c, string field, int val)
        {
            var so = new SerializedObject(c);
            so.FindProperty(field)?.SetValue(val);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Component c, string field, float val)
        {
            var so = new SerializedObject(c);
            var p  = so.FindProperty(field);
            if (p != null) p.floatValue = val;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetEnum(Component c, string field, int val)
        {
            var so = new SerializedObject(c);
            var p  = so.FindProperty(field);
            if (p != null) p.enumValueIndex = val;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetString(Component c, string field, string val)
        {
            var so = new SerializedObject(c);
            var p  = so.FindProperty(field);
            if (p != null) p.stringValue = val;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetVector3(Component c, string field, Vector3 val)
        {
            var so = new SerializedObject(c);
            var p  = so.FindProperty(field);
            if (p != null) p.vector3Value = val;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetComponent(Component c, string field, Object val)
        {
            var so = new SerializedObject(c);
            var p  = so.FindProperty(field);
            if (p != null) p.objectReferenceValue = val;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetObjectArray(Component c, string field, Object[] vals)
        {
            var so   = new SerializedObject(c);
            var prop = so.FindProperty(field);
            if (prop == null) return;
            prop.arraySize = vals.Length;
            for (int i = 0; i < vals.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = vals[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    /// <summary>Extension to avoid a null-check pattern on SerializedProperty int.</summary>
    internal static class SerializedPropertyExt
    {
        internal static void SetValue(this SerializedProperty p, int val)
        {
            if (p.propertyType == SerializedPropertyType.Integer)
                p.intValue = val;
            else if (p.propertyType == SerializedPropertyType.Enum)
                p.enumValueIndex = val;
        }
    }
}
