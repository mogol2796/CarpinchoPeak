using UnityEngine;
using OvrCharacterController = Oculus.Interaction.Locomotion.CharacterController;

public class FallDamage : MonoBehaviour
{
    [Header("Refs")]
    public PlayerManager player;
    public OvrCharacterController ovrCharacterController;
    public Transform trackedRoot; // normalmente el mismo que "playerRoot" del rig
    public ClimbManager climbManager;

    [Header("Filtering")]
    [Tooltip("Evita daño por micro-pérdidas de ground (esquinas, colisiones, rampas).")]
    public float minAirTime = 0.12f;
    [Tooltip("Clamp por seguridad ante teleports o saltos de tracking.")]
    public float maxReasonableSpeed = 30f;

    public float safeImpactSpeed = 6.5f;     // ajusta (m/s aprox)
    public float lethalImpactSpeed = 14.0f;  // ajusta

    [Header("Damage")]
    public float maxDamage = 80f;            // daño máximo al llegar a lethalImpactSpeed

    private float _lastY;
    private bool _hasLastY;
    private float _airTime;
    private float _maxDownSpeed;
    private bool _wasGrounded = true;

    void Awake()
    {
        // Reset() no corre en runtime; intentamos auto-rellenar por si olvidaste asignar refs.
        if (!player) player = FindFirstObjectByType<PlayerManager>();
        if (!ovrCharacterController) ovrCharacterController = FindFirstObjectByType<OvrCharacterController>();
        if (!climbManager) climbManager = FindFirstObjectByType<ClimbManager>();
        if (!trackedRoot && climbManager && climbManager.playerRoot) trackedRoot = climbManager.playerRoot;
    }

    void Reset()
    {
        player = FindFirstObjectByType<PlayerManager>();
        ovrCharacterController = FindFirstObjectByType<OvrCharacterController>();
        climbManager = FindFirstObjectByType<ClimbManager>();
        if (!trackedRoot && climbManager && climbManager.playerRoot) trackedRoot = climbManager.playerRoot;
    }

    void Update()
    {
        if (!player || !ovrCharacterController) return;

        if (!trackedRoot && climbManager && climbManager.playerRoot) trackedRoot = climbManager.playerRoot;
        if (!trackedRoot) trackedRoot = transform;

        bool grounded = ovrCharacterController.IsGrounded;

        // Mientras escalas o el collider aún está en transición, ignora fall damage
        // (evita daño "fantasma" por colisiones/ground jitter o por el resize del capsule).
        if (climbManager && (climbManager.IsClimbing || climbManager.IsMantling || climbManager.IsClimbColliderResizing))
        {
            _airTime = 0f;
            _maxDownSpeed = 0f;
            _wasGrounded = grounded;
            _lastY = trackedRoot.position.y;
            _hasLastY = true;
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            _wasGrounded = grounded;
            return;
        }

        float y = trackedRoot.position.y;
        if (!_hasLastY)
        {
            _lastY = y;
            _hasLastY = true;
            _wasGrounded = grounded;
            return;
        }

        float vy = (y - _lastY) / dt;
        _lastY = y;

        if (!grounded)
        {
            _airTime += dt;

            if (vy < 0f)
            {
                float down = Mathf.Min(-vy, maxReasonableSpeed);
                if (down > _maxDownSpeed) _maxDownSpeed = down;
            }
        }

        // transición: aire -> suelo = impacto
        if (!_wasGrounded && grounded)
        {
            if (_airTime >= minAirTime)
                ApplyFallDamage(_maxDownSpeed);

            _airTime = 0f;
            _maxDownSpeed = 0f;
        }

        if (grounded)
        {
            _airTime = 0f;
            _maxDownSpeed = 0f;
        }

        _wasGrounded = grounded;
    }

    void ApplyFallDamage(float impactSpeed)
    {
        if (impactSpeed <= safeImpactSpeed) return;

        float t = Mathf.InverseLerp(safeImpactSpeed, lethalImpactSpeed, impactSpeed);
        float dmg = Mathf.Lerp(0f, maxDamage, t);

        player.TakeDamage(dmg);
    }
}
