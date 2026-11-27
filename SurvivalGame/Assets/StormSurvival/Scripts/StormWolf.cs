using UnityEngine;
using UnityEngine.AI;

namespace StormSurvival
{
    public enum StormWolfPhase
    {
        Patrol, Warning, Chase, Windup, Recover, Staggered, Retreat, Gone
    }

    public sealed class StormWolf : MonoBehaviour
    {
        private const float WarningRange = 15f;
        private const float ChaseBoundary = 27f;
        private const float PatrolSpeed = 2.65f;
        private const float ChaseSpeed = 7.05f;
        private const float TorchRange = 5.5f;
        private const float LungeStartRange = 1.85f;
        private const float BiteRange = 2.2f;
        private const float WindupTime = .72f;
        private readonly RaycastHit[] sightHits = new RaycastHit[12];

        private StormGame game;
        private StormWolfModel model;
        private Collider body;
        private NavMeshAgent agent;
        private NavMeshPath path;
        private NavMeshQueryFilter navigation;
        private System.Random random;
        private Vector3 home, crossing;
        private Vector2 patrolSpan;
        private float phaseEnds, hiddenUntil, nextPathAt, noticeAfter;
        private int hits, patrolSeed;
        private bool gaveWarning, crossingPending;

        public StormWolfPhase Phase { get; private set; }
        public bool CanTarget => Phase != StormWolfPhase.Retreat && Phase != StormWolfPhase.Gone;
        public string Prompt => game.Player.Inventory.selected == 2
            ? "Left click / fend off the wolf"
            : "Face the wolf with a lit torch, or press 3 for the pickaxe";

        internal static StormWolf Build(StormGame game, StormWorld world, Transform parent, string territory,
            Vector2 span, (Vector2 Home, Vector2 Crossing) site, int seed)
        {
            var filter = new NavMeshQueryFilter { agentTypeID = StormWorld.WolfAgentType, areaMask = NavMesh.AllAreas };
            var path = new NavMeshPath();
            if (!NavMesh.SamplePosition(Ground(site.Home), out var spawn, 2f, filter) ||
                !NavMesh.SamplePosition(Ground(site.Crossing), out var otherSide, 2f, filter) ||
                !NavMesh.CalculatePath(spawn.position, otherSide.position, filter, path) ||
                path.status != NavMeshPathStatus.PathComplete)
                return null;

            var animal = new GameObject("Forest wolf / " + territory);
            animal.transform.SetParent(parent, false);
            animal.transform.position = spawn.position;
            var agent = animal.AddComponent<NavMeshAgent>();
            agent.agentTypeID = filter.agentTypeID;
            agent.radius = .48f;
            agent.height = 1.45f;
            agent.acceleration = 19f;
            agent.angularSpeed = 520f;
            agent.autoTraverseOffMeshLink = false;
            agent.updateUpAxis = false;
            agent.updateRotation = false;

            var body = animal.AddComponent<CapsuleCollider>();
            body.direction = 2;
            body.center = new Vector3(0f, .67f, .13f);
            body.radius = .40f;
            body.height = 1.95f;

            var wolf = animal.AddComponent<StormWolf>();
            wolf.game = game;
            wolf.body = body;
            wolf.agent = agent;
            wolf.path = path;
            wolf.navigation = filter;
            wolf.home = spawn.position;
            wolf.crossing = otherSide.position;
            wolf.patrolSpan = span;
            wolf.patrolSeed = seed;
            wolf.model = animal.AddComponent<StormWolfModel>();
            wolf.model.Build(world);
            wolf.ResetEncounter();
            return wolf;
        }

        public void ResetEncounter()
        {
            if (agent.Warp(home)) StopMoving();
            transform.rotation = Quaternion.LookRotation(Flat(crossing - home));
            body.enabled = true;
            model.SetVisible(true);
            Phase = StormWolfPhase.Patrol;
            random = new System.Random(patrolSeed);
            hits = 0;
            gaveWarning = false;
            crossingPending = true;
            noticeAfter = 0f;
            nextPathAt = 0f;
        }

        public void Strike(Vector3 hitPoint)
        {
            if (!game.Running || !CanTarget || game.Player.Inventory.selected != 2) return;

            game.Audio.Play("wolf stagger", hitPoint, .65f);
            game.Player.Shake(.055f);
            hits++;
            if (hits >= 2)
            {
                BackOff(game.Player.transform.position, "The wolf fled into the trees.");
                return;
            }

            StopMoving();
            Phase = StormWolfPhase.Staggered;
            phaseEnds = Time.time + 1.18f;
        }

        private void Update()
        {
            if (!game.Running || game.Needs.Rescued || Phase == StormWolfPhase.Gone || !agent.isOnNavMesh) return;

            var player = game.Player;
            Vector3 playerPosition = player.transform.position;
            Vector3 toPlayer = Flat(playerPosition - transform.position);
            float distance = toPlayer.magnitude;
            if (!gaveWarning && distance < WarningRange && OpenSight(playerPosition))
            {
                gaveWarning = true;
                game.Audio.Play("wolf warning", transform.position, .78f);
                game.Toast("Wolf on the trail. Keep your distance or ready a lit torch.", 5f);
            }

            if (gaveWarning && CanTarget && distance < TorchRange && PresentingTorch(player) && OpenSight(playerPosition))
                BackOff(playerPosition, "The lit torch drove the wolf away.");

            bool canPursue = playerPosition.z > patrolSpan.x && playerPosition.z < patrolSpan.y &&
                Flat(playerPosition - home).sqrMagnitude <= ChaseBoundary * ChaseBoundary &&
                !game.World.NearBurningFire(playerPosition);

            switch (Phase)
            {
                case StormWolfPhase.Patrol:
                    if (gaveWarning) Patrol(player, distance, canPursue);
                    break;
                case StormWolfPhase.Warning:
                    Face(toPlayer);
                    if (!canPursue) ReturnToPatrol();
                    else if (Time.time >= phaseEnds)
                    {
                        if (distance < WarningRange && OpenSight(playerPosition)) BeginChase();
                        else ReturnToPatrol();
                    }
                    break;
                case StormWolfPhase.Chase:
                    Chase(playerPosition, distance, canPursue);
                    break;
                case StormWolfPhase.Windup:
                    Face(toPlayer);
                    if (Time.time >= phaseEnds)
                    {
                        Phase = StormWolfPhase.Recover;
                        phaseEnds = Time.time + .85f;
                        if (canPursue && distance <= BiteRange && AtBiteHeight(playerPosition) && OpenSight(playerPosition))
                            game.FailFromWolf();
                    }
                    break;
                case StormWolfPhase.Staggered:
                case StormWolfPhase.Recover:
                    Face(toPlayer);
                    if (Time.time >= phaseEnds) BeginChase();
                    break;
                case StormWolfPhase.Retreat:
                    if (Time.time >= phaseEnds || (Time.time >= phaseEnds - 2.5f && distance > 12f))
                    {
                        StopMoving();
                        Phase = StormWolfPhase.Gone;
                        model.SetVisible(false);
                    }
                    break;
            }

            Vector3 movement = Flat(agent.velocity);
            if (Phase is StormWolfPhase.Patrol or StormWolfPhase.Chase or StormWolfPhase.Retreat)
                Face(movement);
            model.Animate(movement.magnitude * Time.deltaTime, Phase);
        }

        private void Patrol(StormPlayer player, float distance, bool canPursue)
        {
            float detectionRange = player.Crouched ? 4.5f : player.Controller.IsRunning ? 14f : 11.5f;
            if (canPursue && Time.time >= noticeAfter && distance < detectionRange && OpenSight(player.transform.position))
            {
                StopMoving();
                Phase = StormWolfPhase.Warning;
                phaseEnds = Time.time + .45f;
                return;
            }

            if (agent.pathPending || (agent.hasPath && agent.remainingDistance > .65f) || Time.time < nextPathAt) return;

            if (crossingPending)
            {
                crossingPending = false;
                if (TryMove(crossing, PatrolSpeed, .25f)) return;
            }

            // Keep patrols varied without selecting tiny shifts or routes that end at a wall.
            for (int attempt = 0; attempt < 16; attempt++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Lerp(10f, 17f, (float)random.NextDouble());
                Vector3 next = LimitToForest(home + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
                if (Flat(next - home).sqrMagnitude >= 9f * 9f &&
                    Flat(next - transform.position).sqrMagnitude >= 7f * 7f && TryMove(next, PatrolSpeed, .35f))
                    return;
            }
            nextPathAt = Time.time + .5f;
        }

        private void BeginChase()
        {
            Phase = StormWolfPhase.Chase;
            hiddenUntil = Time.time + 3.5f;
            nextPathAt = 0f;
            game.Toast("The wolf is chasing you. Run or face it with a lit torch.", 3f);
        }

        private void Chase(Vector3 playerPosition, float distance, bool canPursue)
        {
            if (!canPursue)
            {
                ReturnToPatrol();
                return;
            }

            bool seesPlayer = OpenSight(playerPosition);
            if (seesPlayer) hiddenUntil = Time.time + 3.5f;
            else if (Time.time >= hiddenUntil)
            {
                ReturnToPatrol();
                return;
            }

            if (distance <= LungeStartRange && seesPlayer && AtBiteHeight(playerPosition))
            {
                StopMoving();
                Phase = StormWolfPhase.Windup;
                phaseEnds = Time.time + WindupTime;
                game.Audio.Play("wolf warning", transform.position, .88f);
                game.Toast("The wolf is about to lunge!", 2f);
                return;
            }

            if (Time.time >= nextPathAt)
            {
                nextPathAt = Time.time + .18f;
                if (!TryMove(playerPosition, ChaseSpeed, .75f)) StopMoving();
            }
        }

        private void ReturnToPatrol()
        {
            StopMoving();
            Phase = StormWolfPhase.Patrol;
            noticeAfter = Time.time + 1.6f;
            nextPathAt = 0f;
        }

        private void BackOff(Vector3 playerPosition, string notice)
        {
            StopMoving();
            body.enabled = false;
            Phase = StormWolfPhase.Retreat;
            phaseEnds = Time.time + 5f;
            Vector3 away = Flat(transform.position - playerPosition).normalized + Vector3.right * Mathf.Sign(home.x) * .7f;
            var destination = LimitToForest(transform.position + away.normalized * 16f);
            if (!TryMove(destination, ChaseSpeed, .5f))
                TryMove(LimitToForest(home + Vector3.right * Mathf.Sign(home.x) * 13f), ChaseSpeed, .5f);
            game.Toast(notice, 4f);
        }

        private Vector3 LimitToForest(Vector3 point)
        {
            point.x = Mathf.Clamp(point.x, -StormWorld.WolfAreaHalfWidth + 2f, StormWorld.WolfAreaHalfWidth - 2f);
            point.z = Mathf.Clamp(point.z, patrolSpan.x + 2f, patrolSpan.y - 2f);
            point.y = StormWorld.Height(point.z);
            return point;
        }

        private bool TryMove(Vector3 destination, float speed, float stoppingDistance)
        {
            destination.y = StormWorld.Height(destination.z);
            if (!agent.isOnNavMesh || !NavMesh.SamplePosition(destination, out var target, 2f, navigation) ||
                !agent.CalculatePath(target.position, path) || path.status != NavMeshPathStatus.PathComplete)
                return false;

            agent.speed = speed;
            agent.stoppingDistance = stoppingDistance;
            if (!agent.SetPath(path)) return false;
            agent.isStopped = false;
            return true;
        }

        private void StopMoving()
        {
            if (!agent.isOnNavMesh) return;
            agent.isStopped = true;
            agent.ResetPath();
        }

        private bool PresentingTorch(StormPlayer player)
        {
            if (!game.Needs.HasTorch || !game.Needs.TorchLit || game.Needs.TorchFuel <= 0f || player.Inventory.selected != 0)
                return false;
            Vector3 toWolf = transform.position + Vector3.up - player.View.transform.position;
            return Vector3.Dot(player.View.transform.forward, toWolf.normalized) >= .55f;
        }

        private bool OpenSight(Vector3 playerPosition)
        {
            Vector3 animalEye = transform.position + Vector3.up * 1.17f;
            float chestHeight = game.Player.Crouched ? .59f : .95f;
            Vector3 towardPlayer = playerPosition + Vector3.up * chestHeight - animalEye;
            int count = Physics.RaycastNonAlloc(animalEye, towardPlayer.normalized, sightHits, towardPlayer.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var obstacle = sightHits[i].collider.transform;
                if (!obstacle.IsChildOf(transform) && !obstacle.IsChildOf(game.Player.transform))
                    return false;
            }
            return true;
        }

        private bool AtBiteHeight(Vector3 playerPosition) => Mathf.Abs(playerPosition.y - transform.position.y) < 1.2f;

        private void Face(Vector3 direction)
        {
            if (direction.sqrMagnitude < .001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction),
                agent.angularSpeed * Time.deltaTime);
        }

        private static Vector3 Ground(Vector2 point) => new Vector3(point.x, StormWorld.Height(point.y), point.y);
        private static Vector3 Flat(Vector3 vector) => new Vector3(vector.x, 0f, vector.z);
    }
}
