using UnityEngine;
using OvrCharacterController = Oculus.Interaction.Locomotion.CharacterController;

public class FallDamage : MonoBehaviour
{
    [Header("Refs")]
    public PlayerManager player;
    public OvrCharacterController ovrCharacterController;

    [Header("Tuning")]
    public float gravity = 9.81f;

    public float safeImpactSpeed = 6.5f;     // ajusta (m/s aprox)
    public float lethalImpactSpeed = 14.0f;  // ajusta

    [Header("Damage")]
    public float maxDamage = 80f;            // daño máximo al llegar a lethalImpactSpeed

    private float _verticalSpeed = 0f;
    private bool _wasGrounded = true;

    void Reset()
    {
        player = FindFirstObjectByType<PlayerManager>();
        ovrCharacterController = FindFirstObjectByType<OvrCharacterController>();
    }

    void Update()
    {
        if (!player || !ovrCharacterController) return;

        bool grounded = ovrCharacterController.IsGrounded;

        // en el aire: acumula velocidad hacia abajo
        if (!grounded)
        {
            _verticalSpeed -= gravity * Time.deltaTime;
        }

        // transición: aire -> suelo = impacto
        if (!_wasGrounded && grounded)
        {
            float impactSpeed = Mathf.Abs(_verticalSpeed); // velocidad hacia abajo
            ApplyFallDamage(impactSpeed);

            _verticalSpeed = 0f;
        }

        // suelo: mantenemos velocidad 0
        if (grounded)
        {
            _verticalSpeed = 0f;
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
