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

using UnityEngine;
using System.Collections.Generic;

namespace RogueDeal.Combat.Training
{
    [CreateAssetMenu(fileName = "ComboRecording", menuName = "RogueDeal/Combat/Combo Recording")]
    public class ComboRecordingData : ScriptableObject
    {
        public string recordingName;
        public List<RecordedInput> inputs = new List<RecordedInput>();
        
        [Header("Metadata")]
        public float totalDuration;
        public int hitCount;
        public string description;
    }
}
