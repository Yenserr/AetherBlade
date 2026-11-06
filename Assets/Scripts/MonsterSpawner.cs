using System.Collections;
using UnityEngine;

namespace AetherBlade
{
  
    public class MonsterSpawner : MonoBehaviour
    {
        [Header("Spawner")]
        [SerializeField] private GameObject monsterPrefab;
        [SerializeField] private float      respawnDelay  = 4f;
        [SerializeField] private Transform  spawnPoint;

        [Header("VFX")]
        [SerializeField] private ParticleSystem spawnVFX;

        private GameObject currentMonster;

        private void Start()
        {
     
            if (currentMonster == null)
                SpawnMonster();
        }

        private void SpawnMonster()
        {
            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;

            if (spawnVFX != null)
            {
                spawnVFX.transform.position = pos;
                spawnVFX.Play();
            }

            currentMonster = Instantiate(monsterPrefab, pos, Quaternion.identity);

         
            var ai = currentMonster.GetComponent<ShadowMonsterAI>();
            if (ai != null)
                StartCoroutine(WatchMonster());
        }

        private IEnumerator WatchMonster()
        {
        
            while (currentMonster != null)
                yield return new WaitForSeconds(0.5f);

        
            yield return new WaitForSeconds(respawnDelay);
            SpawnMonster();
        }
    }
}
