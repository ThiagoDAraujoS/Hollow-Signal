using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Core.Managers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CRPG.Tests.PlayMode{
    [TestFixture]
    [Description("Tests the full game boot sequence from cold start to BlankMap scene.")]
    public class GameBootIntegrationTests{
        private const string UnitTestSaveSlotName = "UnitTest";
        private const string BlankMapSceneName = "BlankMap";
        private const string BootSceneName = "Boot";
        private const string BootMenuSceneName = "BootMenuScene";
        private const string GameSessionSceneName = "GameSession";

        [OneTimeSetUp]
        public void OneTimeSetUp() => EnsureUnitTestSaveSlot(BlankMapSceneName);

        /// Gracefully unloads all additive scenes while persistent singletons are still alive.
        [UnityOneTimeTearDown]
        public IEnumerator OneTimeTearDown(){
            Task unloadTask = SceneCoordinator.UnloadAllNonBootScenesAsync();
            while (!unloadTask.IsCompleted)
                yield return null;
        }

        /// Creates or overwrites the UnitTest save file on disk targeting the specified map scene.
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

        /// Tests that Boot loads BootMenuScene, accepts a save slot load command, and activates GameSession and BlankMap.
        [UnityTest]
        [Description("Verifies Boot -> BootMenuScene -> UnitTest save slot -> GameSession -> BlankMap sequence.")]
        public IEnumerator BootToBlankMap_ThroughBootMenuAndSaveLoad_Succeeds(){
            EnsureUnitTestSaveSlot(BlankMapSceneName);

            AsyncOperation loadBootOp = SceneManager.LoadSceneAsync(BootSceneName, LoadSceneMode.Single);
            yield return loadBootOp;

            float timeout = 10f;
            float elapsed = 0f;
            while (!SceneManager.GetSceneByName(BootMenuSceneName).isLoaded && elapsed < timeout){
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsTrue(SceneManager.GetSceneByName(BootMenuSceneName).isLoaded);

            var startSessionTask = SceneCoordinator.StartGameSessionAsync(UnitTestSaveSlotName);

            elapsed = 0f;
            timeout = 15f;
            while ((!SceneManager.GetSceneByName(BlankMapSceneName).isLoaded ||
                    GameSessionManager.CurrentMapManager == null) && elapsed < timeout){
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsTrue(SceneManager.GetSceneByName(GameSessionSceneName).isLoaded);
            Assert.IsTrue(SceneManager.GetSceneByName(BlankMapSceneName).isLoaded);
            Assert.AreEqual(BlankMapSceneName, GameSessionManager.CurrentMapManager.gameObject.scene.name);
            Assert.AreEqual(BlankMapSceneName, GameSessionManager.Instance.currentMapName.Value);
        }
    }
}
