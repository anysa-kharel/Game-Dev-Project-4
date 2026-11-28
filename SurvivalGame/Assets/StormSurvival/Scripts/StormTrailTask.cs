using UnityEngine;

namespace StormSurvival
{
    public sealed class StormTrailTask : MonoBehaviour
    {
        public enum Kind { WinchHandle, ServiceWinch, WestBreaker, EastBreaker, RescueRelay }

        private StormBackcountry route;
        private Renderer statusLamp;
        private Material activeStatus;

        public Kind TaskKind { get; private set; }
        public bool CanTarget => route != null && gameObject.activeInHierarchy;
        public string Prompt => route != null ? route.PromptFor(TaskKind) : string.Empty;

        internal void Initialize(StormBackcountry owner, Kind kind, Renderer lamp, Material activeMaterial)
        {
            route = owner;
            TaskKind = kind;
            statusLamp = lamp;
            activeStatus = activeMaterial;
        }

        public void Use()
        {
            if (CanTarget) route.Use(this);
        }

        internal void ShowCompleted()
        {
            if (statusLamp != null) statusLamp.sharedMaterial = activeStatus;
        }
    }
}
