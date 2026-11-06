using UnityEngine;

namespace AetherBlade
{
   
    public interface IDamageable
    {
        /// <param name="amount">Puntos de daño</param>
        /// <param name="sourcePosition">Posición de origen (para knockback)</param>
        void TakeDamage(int amount, Vector2 sourcePosition);
    }
}
