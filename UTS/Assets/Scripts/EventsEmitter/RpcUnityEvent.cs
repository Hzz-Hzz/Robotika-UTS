using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventsEmitter.models;
using UnityEngine;
using UnityEngine.Events;

namespace InterprocessCommunication
{
    [System.Serializable]
    public class AngleRecommendationReceivedEvent : UnityEvent<AngleRecommendationReceivedEventArgs>
    {
    }

    public class RpcUnityEvent: MonoBehaviour
    {
        public AngleRecommendationReceivedEvent AngleRecommendationReceived;

        public long imageVersion = 0;
        private byte[] imageData;

        private RpcFacade _rpcFacade;

        void Start() {
            if (AngleRecommendationReceived == null)
                AngleRecommendationReceived = new AngleRecommendationReceivedEvent();

            _rpcFacade = new RpcFacade();
            _rpcFacade.startListening();
            StartCoroutine(sendImageDataToServerAndGetFeedback());
        }

        public void OnImageUpdated(ImageUpdatedEventArgs imageData) {
            imageVersion++;
            this.imageData = imageData.imageData;
        }

        IEnumerator sendImageDataToServerAndGetFeedback() {
            var currentImageVersion = -1L;

            while (true) {
                if (imageData == null)
                    yield return null;
                if (currentImageVersion == imageVersion)
                    yield return null;
                currentImageVersion = imageVersion;

                var angleRecommendationTask = _rpcFacade.getAngleRecommendation(imageData);
                yield return angleRecommendationTask.toCoroutine();

                var angleRecommendation = angleRecommendationTask.Result;
                if (angleRecommendation == null)
                    continue;
                AngleRecommendationReceived?.Invoke(new AngleRecommendationReceivedEventArgs{
                    recomomendations = angleRecommendation
                });
            }
        }


        void OnApplicationQuit() {
            _rpcFacade.stopListening();
        }

    }

    public static class TaskToCoroutineExtension {
        public static IEnumerator toCoroutine(this Task task) {
            while (!task.IsCompleted) {
                yield return null;
            }

            if (task.IsFaulted) {
                throw task.Exception;
            }
        }
        public static IEnumerator toCoroutine<T>(this Task<T> task) {
            while (!task.IsCompleted) {
                yield return null;
            }

            if (task.IsFaulted) {
                throw task.Exception;
            }
        }

    }
}