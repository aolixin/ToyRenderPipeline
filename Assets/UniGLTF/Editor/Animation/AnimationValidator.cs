﻿using System.Collections.Generic;
using System.Linq;
using UniGLTF.M17N;
using UnityEditor;
using UnityEngine;

namespace UniGLTF
{
    public static class AnimationValidator
    {
        private enum ExporterValidatorMessages
        {
            [LangMsg(Languages.ja, "ExportRootをanimateすることはできません")]
            [LangMsg(Languages.en, "ExportRoot cannot be animated")]
            ROOT_ANIMATED,
        }

        public static IEnumerable<Validation> Validate(GameObject root)
        {
            if (root == null)
            {
                yield break;
            }

            var animationClips = new List<AnimationClip>();
            if (root.TryGetComponent<Animator>(out var animator))
            {
                animationClips = AnimationExporter.GetAnimationClips(animator);
            }
            if (root.TryGetComponent<Animation>(out var animation))
            {
                animationClips = AnimationExporter.GetAnimationClips(animation);
            }

            if (!animationClips.Any())
            {
                yield break;
            }

            foreach (var animationClip in animationClips)
            {
                foreach (var editorCurveBinding in AnimationUtility.GetCurveBindings(animationClip))
                {
                    // is root included in animation?
                    if (string.IsNullOrEmpty(editorCurveBinding.path))
                    {
                        yield return Validation.Error(ExporterValidatorMessages.ROOT_ANIMATED.Msg());
                        yield break;
                    }
                }
            }
        }
    }
}