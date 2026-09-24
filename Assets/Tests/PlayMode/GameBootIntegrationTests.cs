using System.Collections;
using System.IO;
using Core.Managers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CRPG.Tests.PlayMode{
    [TestFixture]
    public class GameBootIntegrationTests{
        private const string UnitTestSaveSlotName = "UnitTest";
        private const string BlankMapSceneName = "BlankMap";
        private const string BootSceneName = "Boot";
        private const string BootMenuSceneName = "BootMenuScene";
        private const string GameSessionSceneName = "GameSession";

        [OneTimeSetUp]
        public void OneTimeSetUp(){
            EnsureUnitTestSaveSlot(BlankMapSceneName);
        }

        /// <summary>
        /// Creates or overwrites the "UnitTest" save file on disk with partition data
        /// targeting the specified map scene and default party state.
        /// </summary>
        public static void EnsureUnitTestSaveSlot(string targetMapName = BlankMapSceneName, string mainHeroName = "Lucca"){
            string savesDirectory = Path.Combine(Application.persistentDataPath, "Saves");
            string slotDirectory = Path.Combine(savesDirectory, UnitTestSaveSlotName);

            if (!Directory.Exists(slotDirectory))
                Directory.CreateDirectory(slotDirectory);

            string coreJsonPath = Path.Combine(slotDirectory, "core.json");
            string coreJsonContent = "{\n" +
                                     "  \"bf3c39d7-8563-4864-a04b-2162e2838c61\": {\n" +
                                     $"    \"CurrentMapName\": \"{targetMapName}\",\n" +
                                     $"    \"MainCharacterName\": \"{mainHeroName}\",\n" +
                                     "    \"ActiveParty\": [\n" +
                                     "      \"7ae2f656-4d59-4126-a2d1-8aaa45dc4aeb\",\n" +
                                     "      \"eda2a7af-4490-42d9-acca-ec54d72920de\"\n" +
                                     "    ]\n" +
                                     "  }\n" +
                                     "}";
            File.WriteAllText(coreJsonPath, coreJsonContent);

            string metaJsonPath = Path.Combine(slotDirectory, "meta.json");
            string metaJsonContent = "{\n" +
                                     $"  \"slotName\": \"{UnitTestSaveSlotName}\",\n" +
                                     "  \"lastSaveTime\": \"2026-09-24T12:00:00\",\n" +
                                     $"  \"location\": \"{targetMapName}\",\n" +
                                     $"  \"characterName\": \"{mainHeroName}\",\n" +
                                     "  \"region\": \"Test Zone\"\n" +
                                     "}";
            File.WriteAllText(metaJsonPath, metaJsonContent);
        }

        [UnityTest]
        public IEnumerator BootToBlankMap_ThroughBootMenuAndSaveLoad_Succeeds(){
            // 1. Prepare save slot pointing to BlankMap
            EnsureUnitTestSaveSlot(BlankMapSceneName);

            // 2. Load the main Boot scene
            AsyncOperation loadBootOp = SceneManager.LoadSceneAsync(BootSceneName, LoadSceneMode.Single);
            Assert.IsNotNull(loadBootOp, "Failed to initiate loading of Boot scene.");
            yield return loadBootOp;

            // 3. Wait for Boot to auto-load the BootMenuScene
            float timeout = 10f;
            float elapsed = 0f;
            while (!SceneManager.GetSceneByName(BootMenuSceneName).isLoaded && elapsed < timeout){
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsTrue(
                SceneManager.GetSceneByName(BootMenuSceneName).isLoaded,
                $"Timed out waiting for {BootMenuSceneName} to load."
            );
            Assert.IsNotNull(SceneCoordinator.Instance, "SceneCoordinator instance should be initialized.");

            // 4. Issue command to load the UnitTest save slot
            var startSessionTask = SceneCoordinator.StartGameSessionAsync(UnitTestSaveSlotName);

            // 5. Wait for the GameSession scene and the target BlankMap scene to load
            elapsed = 0f;
            timeout = 15f;
            while ((!SceneManager.GetSceneByName(BlankMapSceneName).isLoaded ||
                    GameSessionManager.CurrentMapManager == null) && elapsed < timeout){
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // 6. Assertions for successful boot & transition
            Assert.IsTrue(
                SceneManager.GetSceneByName(GameSessionSceneName).isLoaded,
                $"{GameSessionSceneName} scene should be loaded."
            );
            Assert.IsTrue(
                SceneManager.GetSceneByName(BlankMapSceneName).isLoaded,
                $"{BlankMapSceneName} scene should be loaded."
            );
            Assert.IsNotNull(
                GameSessionManager.Instance,
                "GameSessionManager.Instance should be initialized."
            );
            Assert.IsNotNull(
                GameSessionManager.CurrentMapManager,
                "GameSessionManager.CurrentMapManager should be assigned."
            );
            Assert.AreEqual(
                BlankMapSceneName,
                GameSessionManager.CurrentMapManager.gameObject.scene.name,
                "CurrentMapManager does not match BlankMap scene."
            );
            Assert.AreEqual(
                BlankMapSceneName,
                GameSessionManager.Instance.currentMapName.Value,
                "GameSessionManager currentMapName does not match BlankMap."
            );
        }
    }
}
