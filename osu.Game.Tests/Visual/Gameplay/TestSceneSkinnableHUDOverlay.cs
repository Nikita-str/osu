// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Play;
using osu.Game.Screens.Play.HUD;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Gameplay
{
    public class TestSceneSkinnableHUDOverlay : SkinnableTestScene
    {
        private HUDOverlay hudOverlay;

        private IEnumerable<HUDOverlay> hudOverlays => CreatedDrawables.OfType<HUDOverlay>();

        // best way to check without exposing.
        private Drawable hideTarget => hudOverlay.KeyCounter;
        private FillFlowContainer<KeyCounter> keyCounterFlow => hudOverlay.KeyCounter.ChildrenOfType<FillFlowContainer<KeyCounter>>().First();

        [Resolved]
        private OsuConfigManager config { get; set; }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Width = 0.3f,
                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.Black,
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0.7f,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Children = createSkinSourceComponents(),
                    },
                }
            });
        }

        [Test]
        public void TestComboCounterIncrementing()
        {
            createNew();

            AddRepeatStep("increase combo", () =>
            {
                foreach (var hud in hudOverlays)
                    hud.ComboCounter.Current.Value++;
            }, 10);

            AddStep("reset combo", () =>
            {
                foreach (var hud in hudOverlays)
                    hud.ComboCounter.Current.Value = 0;
            });
        }

        [Test]
        public void TestFadesInOnLoadComplete()
        {
            float? initialAlpha = null;

            createNew(h => h.OnLoadComplete += _ => initialAlpha = hideTarget.Alpha);
            AddUntilStep("wait for load", () => hudOverlay.IsAlive);
            AddAssert("initial alpha was less than 1", () => initialAlpha < 1);
        }

        [Test]
        public void TestHideExternally()
        {
            createNew();

            AddStep("set showhud false", () => hudOverlays.ForEach(h => h.ShowHud.Value = false));

            AddUntilStep("hidetarget is hidden", () => !hideTarget.IsPresent);
            AddAssert("pause button is still visible", () => hudOverlay.HoldToQuit.IsPresent);

            // Key counter flow container should not be affected by this, only the key counter display will be hidden as checked above.
            AddAssert("key counter flow not affected", () => keyCounterFlow.IsPresent);
        }

        private IReadOnlyList<Drawable> createSkinSourceComponents()
        {
            var hudComponents = typeof(SkinnableHUDComponent).Assembly.GetTypes().Where(t => typeof(SkinnableHUDComponent).IsAssignableFrom(t)).ToArray();

            List<Drawable> drawables = new List<Drawable>();

            foreach (var component in hudComponents)
                drawables.AddRange(component.CreateSettingsControls());

            return drawables;
        }

        private void createNew(Action<HUDOverlay> action = null)
        {
            AddStep("create overlay", () =>
            {
                SetContents(() =>
                {
                    hudOverlay = new HUDOverlay(null, null, null, Array.Empty<Mod>());

                    // Add any key just to display the key counter visually.
                    hudOverlay.KeyCounter.Add(new KeyCounterKeyboard(Key.Space));

                    hudOverlay.ComboCounter.Current.Value = 1;

                    action?.Invoke(hudOverlay);

                    return hudOverlay;
                });
            });
        }

        protected override Ruleset CreateRulesetForSkinProvider() => new OsuRuleset();
    }
}
