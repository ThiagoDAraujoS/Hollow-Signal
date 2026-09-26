using System.Collections;
using System.Threading.Tasks;
using Core.Managers;
using NUnit.Framework;
using UI.TurnTable;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CRPG.Tests.PlayMode{
    [TestFixture]
    [Description("Interactive visual verification of TurnTable Crisis Clock states, transitions, and extractions.")]
    public class CrisisClockVisualVerificationTests{
        private const string BlankMapScene = "BlankMap";
        private const string BootScene     = "Boot";

        /// Boots the game into BlankMap so GameSession and TurnTable are live.
        [UnityOneTimeSetUp]
        public IEnumerator OneTimeSetUp(){
            if (SceneManager.GetSceneByName(BlankMapScene).isLoaded && GameSessionManager.CurrentMapManager != null)
                yield break;

            GameBootIntegrationTests.EnsureUnitTestSaveSlot(BlankMapScene);
            AsyncOperation loadBoot = SceneManager.LoadSceneAsync(BootScene, LoadSceneMode.Single);
            yield return loadBoot;

            float elapsed = 0f;
            while (!SceneManager.GetSceneByName("BootMenuScene").isLoaded && elapsed < 10f){
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            SceneCoordinator.StartGameSessionAsync("UnitTest");

            elapsed = 0f;
            while ((!SceneManager.GetSceneByName(BlankMapScene).isLoaded ||
                    GameSessionManager.CurrentMapManager == null) && elapsed < 15f){
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// Gracefully unloads all additive scenes while persistent singletons are still alive.
        [UnityOneTimeTearDown]
        public IEnumerator OneTimeTearDown(){
            Task unloadTask = SceneCoordinator.UnloadAllNonBootScenesAsync();
            while (!unloadTask.IsCompleted)
                yield return null;
        }

        /// Helper waiting for user confirmation on the overlay prompt.
        private IEnumerator AskConfirmation(TestPromptOverlay overlay, string stepText, string failReason){
            overlay.PromptText = stepText + "\nClick button or press [Y] / [N]";
            overlay.UserChoice = null;

            while (!overlay.UserChoice.HasValue)
                yield return null;

            Assert.IsTrue(overlay.UserChoice.Value, failReason);
            yield return new WaitForSeconds(0.2f);
        }

        /// Tests complete sequence of opening, flipping, and extracting the TurnTable Crisis Clock with visual prompts.
        [UnityTest]
        [Description("Verifies TurnTable open to enemy, switch to ally, extract, open to ally, flip to enemy, and extract.")]
        public IEnumerator LiveCrisisClock_StateTransitions_UserVisuallyConfirms(){
            TurnTableController clock = Object.FindAnyObjectByType<TurnTableController>();
            GameObject overlayObj = new("ClockTestPromptOverlay");
            TestPromptOverlay overlay = overlayObj.AddComponent<TestPromptOverlay>();

            // 1. Open pointing at enemy (Red)
            clock.animator.SetBool(clock.isOnParam, true);
            clock.animator.SetBool(clock.isRedParam, true);
            yield return new WaitForSeconds(1.5f);
            yield return AskConfirmation(overlay, "[Step 1/6] Is the Crisis Clock open and pointing at ENEMY (Red)?", "Step 1 Failed: Clock did not open pointing at Enemy.");

            // 2. Move pointer to ally (Green)
            clock.animator.SetBool(clock.isRedParam, false);
            yield return new WaitForSeconds(1.2f);
            yield return AskConfirmation(overlay, "[Step 2/6] Did the pointer move from Enemy to ALLY (Green)?", "Step 2 Failed: Pointer did not transition to Ally.");

            // 3. Extract clock
            clock.animator.SetBool(clock.isOnParam, false);
            yield return new WaitForSeconds(1.5f);
            yield return AskConfirmation(overlay, "[Step 3/6] Was the Crisis Clock retracted / extracted?", "Step 3 Failed: Clock did not retract.");

            // 4. Bring back pointing at ally (Green)
            clock.animator.SetBool(clock.isOnParam, true);
            clock.animator.SetBool(clock.isRedParam, false);
            yield return new WaitForSeconds(1.5f);
            yield return AskConfirmation(overlay, "[Step 4/6] Did the Crisis Clock return pointing at ALLY (Green)?", "Step 4 Failed: Clock did not return pointing at Ally.");

            // 5. Flip pointer to enemy (Red)
            clock.animator.SetBool(clock.isRedParam, true);
            yield return new WaitForSeconds(1.2f);
            yield return AskConfirmation(overlay, "[Step 5/6] Did the pointer flip to ENEMY (Red)?", "Step 5 Failed: Pointer did not flip to Enemy.");

            // 6. Extract clock
            clock.animator.SetBool(clock.isOnParam, false);
            yield return new WaitForSeconds(1.5f);
            yield return AskConfirmation(overlay, "[Step 6/6] Was the Crisis Clock retracted / extracted?", "Step 6 Failed: Clock did not retract.");

            Object.Destroy(overlayObj);
        }
    }
}
