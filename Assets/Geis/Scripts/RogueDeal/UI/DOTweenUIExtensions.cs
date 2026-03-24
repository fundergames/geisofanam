using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

namespace RogueDeal.UI
{
    /// <summary>
    /// Custom DOTween UI extension methods for CanvasGroup and Graphic types.
    /// These are needed when DOTween's built-in UI module is not accessible due to assembly definition issues.
    /// NOTE: These extensions are in RogueDeal.UI namespace to avoid conflicts with DOTween's built-in extensions.
    /// </summary>
    public static class DOTweenUIExtensions
    {
        public static TweenerCore<float, float, FloatOptions> DOFade(this CanvasGroup target, float endValue, float duration)
        {
            TweenerCore<float, float, FloatOptions> t = DOTween.To(() => target.alpha, x => target.alpha = x, endValue, duration);
            t.SetTarget(target);
            return t;
        }

        public static TweenerCore<Color, Color, ColorOptions> DOFade(this Graphic target, float endValue, float duration)
        {
            TweenerCore<Color, Color, ColorOptions> t = DOTween.ToAlpha(() => target.color, x => target.color = x, endValue, duration);
            t.SetTarget(target);
            return t;
        }
    }
}
