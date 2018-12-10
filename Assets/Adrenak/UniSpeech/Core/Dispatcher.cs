using System;
using System.Collections.Generic;
using UnityEngine;

namespace Adrenak.UniSpeech {
	public class Dispatcher : MonoBehaviour {
		static Dispatcher m_Instance;

		public static void Create () {
			if (m_Instance == null)
				m_Instance = GameObject.FindObjectOfType<Dispatcher>();
			if(m_Instance == null) {
				var go = new GameObject {
					hideFlags = HideFlags.HideAndDontSave
				};
				DontDestroyOnLoad(go);
				m_Instance = go.AddComponent<Dispatcher>();
			}
		}

		Queue<Action> m_Queue = new Queue<Action>();
		
		public static void Enqueue(Action action) {
			lock (m_Instance.m_Queue) {
				m_Instance.m_Queue.Enqueue(action);
			}
		}

		void Update() {
			lock (m_Queue) {
				while (m_Queue.Count > 0)
					m_Queue.Dequeue()();
			}
		}
	}
}
