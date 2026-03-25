using System.Collections.Generic;
using UnityEngine;

namespace Hero.Combat.Manager
{
    // 심플한 오브젝트 풀 클래스. Instantiate/Destroy 부하를 줄이기 위함
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance { get; private set; }

        [SerializeField] private GameObject _agentPrefab;
        [SerializeField] private int _initialPoolSize = 500;
        
        private Queue<GameObject> _poolQueue = new Queue<GameObject>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < _initialPoolSize; i++)
            {
                CreateNewObject();
            }
        }

        private void CreateNewObject()
        {
            GameObject obj = Instantiate(_agentPrefab, transform);
            obj.SetActive(false);
            _poolQueue.Enqueue(obj);
        }

        public GameObject Spawn(Vector3 position, Quaternion rotation)
        {
            if (_poolQueue.Count == 0)
            {
                CreateNewObject();
            }

            GameObject obj = _poolQueue.Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            return obj;
        }

        public void Despawn(GameObject obj)
        {
            obj.SetActive(false);
            _poolQueue.Enqueue(obj);
        }
    }
}
