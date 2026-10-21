using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BlackboardSaveSystem;
using Core.Managers;
using Tests.PlayMode.TestComponents;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.PlayMode{
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
        public IEnumerator Entity_SpawnAndDespawn_SynchronizesWithBlackboard() {
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

            Assert.AreEqual(100, (int)SaveSystem.Blackboard.GetPartition("core", testGuid, "0")["Health"]);
            Assert.AreEqual("Alive", (string)SaveSystem.Blackboard.GetPartition("core", testGuid, "0")["Status"]);

            tracked.Health.Value = 42;
            tracked.Status.Value = "Wounded";

            entityObj.SetActive(false);

            var partition = SaveSystem.Blackboard.GetPartition("core", testGuid, "0");
            Assert.AreEqual(42, (int)partition["Health"]);
            Assert.AreEqual("Wounded", (string)partition["Status"]);

            UnityEngine.Object.Destroy(entityObj);
            yield return null;
        }

        /// Tests that two TrackedBehaviour components on the same GameObject with identical Tracked keys are isolated by component order index.
        [UnityTest]
        [Description("Verifies two TrackedBehaviour components on the same GameObject have isolated component-indexed partitions and do not overwrite identical variable names.")]
        public IEnumerator MultipleComponents_WithSameVariableNames_DoNotCollide(){
            GameObject entityObj = new("MultiComponentEntity");
            entityObj.SetActive(false);

            UniqueId uniqueId = entityObj.AddComponent<UniqueId>();
            FieldInfo idField = typeof(UniqueId).GetField("uniqueId", BindingFlags.NonPublic | BindingFlags.Instance);
            string testGuid = Guid.NewGuid().ToString();
            idField.SetValue(uniqueId, testGuid);

            BlackboardClient client = entityObj.AddComponent<BlackboardClient>();
            client.fileName = "core";

            TestTrackedEntity comp0 = entityObj.AddComponent<TestTrackedEntity>();
            TestTrackedEntity comp1 = entityObj.AddComponent<TestTrackedEntity>();

            entityObj.SetActive(true);

            comp0.Health.Value = 100;
            comp0.Status.Value = "PrimaryAlive";
            comp1.Health.Value = 250;
            comp1.Status.Value = "SecondaryShielded";

            entityObj.SetActive(false);

            var partition0 = SaveSystem.Blackboard.GetPartition("core", testGuid, "0");
            var partition1 = SaveSystem.Blackboard.GetPartition("core", testGuid, "1");

            Assert.AreEqual(100, (int)partition0["Health"]);
            Assert.AreEqual("PrimaryAlive", (string)partition0["Status"]);
            Assert.AreEqual(250, (int)partition1["Health"]);
            Assert.AreEqual("SecondaryShielded", (string)partition1["Status"]);

            comp0.Health.Value = 0;
            comp1.Health.Value = 0;

            entityObj.SetActive(true);

            Assert.AreEqual(100, comp0.Health.Value);
            Assert.AreEqual("PrimaryAlive", comp0.Status.Value);
            Assert.AreEqual(250, comp1.Health.Value);
            Assert.AreEqual("SecondaryShielded", comp1.Status.Value);

            UnityEngine.Object.Destroy(entityObj);
            yield return null;
        }

        /// Tests the full save/load disk cycle: RAM -> _active_session -> Saves/slot -> _active_session -> RAM.
        [UnityTest]
        [Description("Verifies SaveSystem.SaveGame flushes RAM to disk slot and SaveSystem.LoadFiles restores state into RAM.")]
        public IEnumerator SaveAndLoadGame_PreservesDiskAndMemoryIntegrity(){
            var partition = SaveSystem.Blackboard.GetPartition("core", "disk-test-entity", "0");
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

            var restoredPartition = SaveSystem.Blackboard.GetPartition("core", "disk-test-entity", "0");
            Assert.AreEqual(999, (int)restoredPartition["Coins"]);

            SaveSystem.DeleteSave(TestSlotName);
        }

        /// Tests scene RAII: loads scene partition files into RAM and releases/purges them on transition.
        [UnityTest]
        [Description("Verifies scene partition loading into Blackboard RAM and purging via CommitFileAsync and ReleaseFile.")]
        public IEnumerator SceneDependency_LoadsAndPurgesBlackboardFiles(){
            Task loadTask = SaveSystem.LoadFiles(new[]{ "custom_dungeon" });
            while (!loadTask.IsCompleted)
                yield return null;

            Assert.IsTrue(SaveSystem.Blackboard.Contains("custom_dungeon"));

            var dungeonPart = SaveSystem.Blackboard.GetPartition("custom_dungeon", "chest-01", "0");
            dungeonPart["IsOpen"] = true;

            Task commitTask = SaveSystem.CommitFileAsync("custom_dungeon");
            while (!commitTask.IsCompleted)
                yield return null;

            SaveSystem.ReleaseFile("custom_dungeon");

            Assert.IsFalse(SaveSystem.Blackboard.Contains("custom_dungeon"));

            string activeDungeonFile = Path.Combine(SaveSystem.ActiveSessionDirectory, "custom_dungeon.json");
            Assert.IsTrue(File.Exists(activeDungeonFile));
            Assert.IsTrue(File.ReadAllText(activeDungeonFile).Contains("\"IsOpen\": true"));
        }

        /// Tests that reference counting prevents re-deserialization from overwriting live RAM when refCount > 1
        /// and only serializes + evicts the file from RAM when refCount reaches 0.
        [UnityTest]
        [Description("Verifies reference counting prevents re-deserializing over live RAM state and only flushes/unloads when refCount reaches 0.")]
        public IEnumerator ReferenceCounting_MultipleLoads_DoesNotOverwriteLiveMemory_AndReleasesOnlyAtZero(){
            const string sharedFile = "shared_room_state";

            Task firstLoad = SaveSystem.LoadFiles(new[]{ sharedFile });
            while (!firstLoad.IsCompleted)
                yield return null;

            Assert.AreEqual(1, SaveSystem.GetRefCount(sharedFile));
            Assert.IsTrue(SaveSystem.Blackboard.Contains(sharedFile));

            // Write initial state to disk ("IsLocked": true)
            var partition = SaveSystem.Blackboard.GetPartition(sharedFile, "door-01", "0");
            partition["IsLocked"] = true;

            Task commitTask = SaveSystem.CommitFileAsync(sharedFile);
            while (!commitTask.IsCompleted)
                yield return null;

            // Mutate live state in RAM ("IsLocked": false) without writing to disk yet
            partition["IsLocked"] = false;

            // Second load (e.g. entering an adjacent room that also uses shared_room_state)
            Task secondLoad = SaveSystem.LoadFiles(new[]{ sharedFile });
            while (!secondLoad.IsCompleted)
                yield return null;

            // Ref count must be 2, and live RAM state ("IsLocked": false) must NOT be overwritten by disk ("IsLocked": true)
            Assert.AreEqual(2, SaveSystem.GetRefCount(sharedFile));
            Assert.AreEqual(false, (bool)SaveSystem.Blackboard.GetPartition(sharedFile, "door-01", "0")["IsLocked"]);

            // First release (leaving one room while the other still holds a reference)
            Task<bool> firstRelease = SaveSystem.ReleaseFileAsync(sharedFile);
            while (!firstRelease.IsCompleted)
                yield return null;

            Assert.IsFalse(firstRelease.Result);
            Assert.AreEqual(1, SaveSystem.GetRefCount(sharedFile));
            Assert.IsTrue(SaveSystem.Blackboard.Contains(sharedFile));
            Assert.AreEqual(false, (bool)SaveSystem.Blackboard.GetPartition(sharedFile, "door-01", "0")["IsLocked"]);

            // Second release (last reference released -> serializes to _active_session and removes from RAM)
            Task<bool> secondRelease = SaveSystem.ReleaseFileAsync(sharedFile);
            while (!secondRelease.IsCompleted)
                yield return null;

            Assert.IsTrue(secondRelease.Result);
            Assert.AreEqual(0, SaveSystem.GetRefCount(sharedFile));
            Assert.IsFalse(SaveSystem.Blackboard.Contains(sharedFile));

            string diskPath = Path.Combine(SaveSystem.ActiveSessionDirectory, $"{sharedFile}.json");
            Assert.IsTrue(File.Exists(diskPath));
            Assert.IsTrue(File.ReadAllText(diskPath).Contains("\"IsLocked\": false"));
        }

        /// Tests BlackboardScope RAII MonoBehaviour acquiring and releasing files on OnEnable/OnDisable.
        [UnityTest]
        [Description("Verifies BlackboardScope acquires files on Enable, shares ref counts across scopes, and flushes/evicts on last release.")]
        public IEnumerator BlackboardScope_EnableAndDisable_ManagesFileReferenceCountsAndEntityHydration(){
            const string scopeFile = "scope_test_file";

            GameObject scopeObjA = new("ScopeA");
            scopeObjA.SetActive(false);
            BlackboardScope scopeA = scopeObjA.AddComponent<BlackboardScope>();
            scopeA.fileNames.Add(scopeFile);

            GameObject scopeObjB = new("ScopeB");
            scopeObjB.SetActive(false);
            BlackboardScope scopeB = scopeObjB.AddComponent<BlackboardScope>();
            scopeB.fileNames.Add(scopeFile);

            GameObject entityObj = new("ScopedEntity");
            entityObj.SetActive(false);
            UniqueId uniqueId = entityObj.AddComponent<UniqueId>();
            FieldInfo idField = typeof(UniqueId).GetField("uniqueId", BindingFlags.NonPublic | BindingFlags.Instance);
            string testGuid = Guid.NewGuid().ToString();
            idField.SetValue(uniqueId, testGuid);

            BlackboardClient client = entityObj.AddComponent<BlackboardClient>();
            client.fileName = scopeFile;
            TestTrackedEntity tracked = entityObj.AddComponent<TestTrackedEntity>();

            // Enable ScopeA -> acquires scopeFile (refCount = 1)
            scopeObjA.SetActive(true);
            Assert.AreEqual(1, SaveSystem.GetRefCount(scopeFile));
            Assert.IsTrue(SaveSystem.Blackboard.Contains(scopeFile));

            // Enable entity and mutate state
            entityObj.SetActive(true);
            tracked.Health.Value = 77;
            tracked.Status.Value = "ScopedAlive";

            // Enable ScopeB -> increments refCount to 2 without overwriting RAM
            scopeObjB.SetActive(true);
            Assert.AreEqual(2, SaveSystem.GetRefCount(scopeFile));

            // Release ScopeB -> drops refCount to 1, stays in RAM
            Task releaseB = scopeB.ReleaseScopeAsync();
            while (!releaseB.IsCompleted)
                yield return null;
            scopeObjB.SetActive(false);

            Assert.AreEqual(1, SaveSystem.GetRefCount(scopeFile));
            Assert.IsTrue(SaveSystem.Blackboard.Contains(scopeFile));

            // Release ScopeA first while entity is still active (verifies OnCollectData flushes client before eviction)
            Task releaseA = scopeA.ReleaseScopeAsync();
            while (!releaseA.IsCompleted)
                yield return null;
            scopeObjA.SetActive(false);
            entityObj.SetActive(false);

            Assert.AreEqual(0, SaveSystem.GetRefCount(scopeFile));
            Assert.IsFalse(SaveSystem.Blackboard.Contains(scopeFile));

            // Mutate in-memory component fields while disabled, then re-enable ScopeA and entity to verify disk hydration
            tracked.Health.Value = 0;
            tracked.Status.Value = string.Empty;

            scopeObjA.SetActive(true);
            entityObj.SetActive(true);

            Assert.AreEqual(1, SaveSystem.GetRefCount(scopeFile));
            Assert.AreEqual(77, tracked.Health.Value);
            Assert.AreEqual("ScopedAlive", tracked.Status.Value);

            // Cleanup
            entityObj.SetActive(false);
            Task finalRelease = scopeA.ReleaseScopeAsync();
            while (!finalRelease.IsCompleted)
                yield return null;
            scopeObjA.SetActive(false);

            UnityEngine.Object.Destroy(entityObj);
            UnityEngine.Object.Destroy(scopeObjA);
            UnityEngine.Object.Destroy(scopeObjB);
            yield return null;
        }
    }
}
