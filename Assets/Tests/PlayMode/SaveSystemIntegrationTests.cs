using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Core.Managers;
using Core.State;
using CRPG.Tests.PlayMode.TestComponents;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CRPG.Tests.PlayMode{
    [TestFixture]
    [Description("Tests Blackboard memory management, TrackedBehaviour synchronization, and SaveSystem disk serialization.")]
    public class SaveSystemIntegrationTests{
        private const string TestSlotName  = "TestSaveSlot";
        private const string BlankMapScene = "BlankMap";
        private const string BootScene     = "Boot";

        /// Boots the game into BlankMap once before any tests in this fixture execute.
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

        /// Tests entity RAII: populates initial values on spawn and flushes mutated Tracked values on despawn.
        [UnityTest]
        [Description("Verifies entity RAII: BlackboardClient loads state on Enable and flushes state to Blackboard on Disable.")]
        public IEnumerator Entity_SpawnAndDespawn_SynchronizesWithBlackboard(){
            GameObject entityObj = new("TestEntity");
            entityObj.SetActive(false);

            UniqueId uniqueId = entityObj.AddComponent<UniqueId>();
            FieldInfo idField = typeof(UniqueId).GetField("uniqueId", BindingFlags.NonPublic | BindingFlags.Instance);
            string testGuid = Guid.NewGuid().ToString();
            idField.SetValue(uniqueId, testGuid);

            BlackboardClient client = entityObj.AddComponent<BlackboardClient>();
            client.fileName = "core";
            TestTrackedEntity tracked = entityObj.AddComponent<TestTrackedEntity>();

            entityObj.SetActive(true);

            Assert.AreEqual(100, (int)SaveSystem.Blackboard.GetPartition("core", testGuid)["Health"]);
            Assert.AreEqual("Alive", (string)SaveSystem.Blackboard.GetPartition("core", testGuid)["Status"]);

            tracked.Health.Value = 42;
            tracked.Status.Value = "Wounded";

            entityObj.SetActive(false);

            var partition = SaveSystem.Blackboard.GetPartition("core", testGuid);
            Assert.AreEqual(42, (int)partition["Health"]);
            Assert.AreEqual("Wounded", (string)partition["Status"]);

            UnityEngine.Object.Destroy(entityObj);
            yield return null;
        }

        /// Tests the full save/load disk cycle: RAM -> _active_session -> Saves/slot -> _active_session -> RAM.
        [UnityTest]
        [Description("Verifies SaveSystem.SaveGame flushes RAM to disk slot and SaveSystem.LoadFiles restores state into RAM.")]
        public IEnumerator SaveAndLoadGame_PreservesDiskAndMemoryIntegrity(){
            var partition = SaveSystem.Blackboard.GetPartition("core", "disk-test-entity");
            partition["Coins"] = 999;

            Task saveTask = SaveSystem.SaveGame(TestSlotName);
            while (!saveTask.IsCompleted)
                yield return null;

            string savedCorePath = Path.Combine(Application.persistentDataPath, "Saves", TestSlotName, "core.json");
            Assert.IsTrue(File.Exists(savedCorePath));

            SaveSystem.ReleaseFile("core");
            Assert.IsFalse(SaveSystem.Blackboard.Contains("core"));

            SaveSystem.InitializeSession(TestSlotName);
            Task loadTask = SaveSystem.LoadFiles(new[]{ "core" });
            while (!loadTask.IsCompleted)
                yield return null;

            var restoredPartition = SaveSystem.Blackboard.GetPartition("core", "disk-test-entity");
            Assert.AreEqual(999, (int)restoredPartition["Coins"]);

            SaveSystem.DeleteSave(TestSlotName);
        }

        /// Tests scene RAII: loads scene partition files into RAM and releases/purges them on transition.
        [UnityTest]
        [Description("Verifies scene partition loading into Blackboard RAM and purging via CommitAndReleaseFileAsync.")]
        public IEnumerator SceneDependency_LoadsAndPurgesBlackboardFiles(){
            Task loadTask = SaveSystem.LoadFiles(new[]{ "custom_dungeon" });
            while (!loadTask.IsCompleted)
                yield return null;

            Assert.IsTrue(SaveSystem.Blackboard.Contains("custom_dungeon"));

            var dungeonPart = SaveSystem.Blackboard.GetPartition("custom_dungeon", "chest-01");
            dungeonPart["IsOpen"] = true;

            Task releaseTask = SaveSystem.CommitAndReleaseFileAsync("custom_dungeon");
            while (!releaseTask.IsCompleted)
                yield return null;

            Assert.IsFalse(SaveSystem.Blackboard.Contains("custom_dungeon"));

            string activeDungeonFile = Path.Combine(SaveSystem.ActiveSessionDirectory, "custom_dungeon.json");
            Assert.IsTrue(File.Exists(activeDungeonFile));
            Assert.IsTrue(File.ReadAllText(activeDungeonFile).Contains("\"IsOpen\": true"));
        }
    }
}
