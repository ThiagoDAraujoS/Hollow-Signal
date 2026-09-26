using System.Collections;
using System.Threading.Tasks;
using Core.Managers;
using NUnit.Framework;
using UI.Dice;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CRPG.Tests.PlayMode{
    /// Immediate mode GUI overlay for manual test verification.
    internal class TestPromptOverlay : MonoBehaviour{
        public string PromptText;
        public bool? UserChoice;

        private void OnGUI(){
            GUI.Box(new Rect(Screen.width * 0.5f - 220, 20, 440, 110), PromptText);
            if (GUI.Button(new Rect(Screen.width * 0.5f - 210, 75, 200, 45), "YES (Y)"))
                UserChoice = true;
            if (GUI.Button(new Rect(Screen.width * 0.5f + 10, 75, 200, 45), "NO (N)"))
                UserChoice = false;

            Event e = Event.current;
            if (e is { type: EventType.KeyDown, keyCode: KeyCode.Y })
                UserChoice = true;
            else if (e is { type: EventType.KeyDown, keyCode: KeyCode.N })
                UserChoice = false;
        }
    }

    [TestFixture]
    [Description("Interactive verification of 3 consecutive dice rolls against visual dice faces.")]
    public class DiceVisualVerificationTests{
        private const string BlankMapScene = "BlankMap";
        private const string BootScene     = "Boot";

        /// Boots the game into BlankMap so GameSession and DiceRollController are live.
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

        /// Executes 3 consecutive dice rolls requiring user confirmation that detected faces match the visual 3D dice.
        [UnityTest]
        [Description("Performs 3 consecutive dice rolls prompting the user to verify detected values match visual faces.")]
        public IEnumerator LiveDiceRoll_ThreeRounds_UserVisuallyConfirmsResults(){
            GameObject overlayObj = new("DiceTestPromptOverlay");
            TestPromptOverlay overlay = overlayObj.AddComponent<TestPromptOverlay>();

            for (int round = 1; round <= 3; round++){
                int[] detected = null;
                bool rollCompleted = false;

                DiceRollController.Roll(results => {
                    detected = results;
                    rollCompleted = true;
                });

                float timeout = 10f;
                float elapsed = 0f;
                while (!rollCompleted && elapsed < timeout){
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                Assert.IsTrue(rollCompleted, $"Round {round}: Dice roll timed out.");

                string valuesString = string.Join(", ", detected);
                overlay.PromptText = $"[Round {round}/3] Detected: [ {valuesString} ]\nDo visual 3D dice match these values?\nClick button or press [Y] / [N]";
                overlay.UserChoice = null;

                while (!overlay.UserChoice.HasValue)
                    yield return null;

                Assert.IsTrue(overlay.UserChoice.Value, $"Dice mismatch reported on Round {round}! Detected: [{valuesString}]");
                yield return new WaitForSeconds(0.3f);
            }

            Object.Destroy(overlayObj);
        }
    }
}
