using UnityEngine;

namespace StormSurvival
{
    public sealed class StormMineReward : MonoBehaviour
    {
        public bool bridgeObstacle;
        public int stone = 4;
        private Mineable mine;
        private void Awake() { mine = GetComponent<Mineable>(); }
        public void Bind()
        {
            if (mine == null) mine = GetComponent<Mineable>();
            mine.Depleted -= Reward; mine.Depleted += Reward;
        }
        private void Reward(Mineable source)
        {
            var game = StormGame.Instance;
            if (game == null) return;
            game.Needs.Stone += stone;
            game.Audio.Play("collect", transform.position, 0.5f);
            game.Toast("+" + stone + " stone / " + game.Needs.Stone + " carried");
            if (bridgeObstacle) game.World.DropBridge();
        }
        private void OnDestroy() { if (mine != null) mine.Depleted -= Reward; }
    }
}
