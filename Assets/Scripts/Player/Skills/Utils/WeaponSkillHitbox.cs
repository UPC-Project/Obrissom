using UnityEngine;
using Obrissom.Enemy;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
using Obrissom.Player;

public enum SkillHitboxType { Weapon, SkillZone }

[RequireComponent(typeof(Collider))]
public class WeaponSkillHitbox : MonoBehaviour
{
    public SkillHitboxType hitboxType = SkillHitboxType.Weapon;

    private Skill _currentSkill;
    private PlayerCombat _playerCombat;
    private NetworkObject _netObj;

    private HashSet<EnemyBase> _hitEnemies = new HashSet<EnemyBase>();
    private bool _hitMultiple;
    private bool _isActive;
    
    private bool _applyInitialDamage = false;
    private bool _applyDOT = false;

    private void Awake()
    {
        _playerCombat = GetComponentInParent<PlayerCombat>();
        _netObj = GetComponentInParent<NetworkObject>();
    }

    public void SetupSkill(Skill skill, bool hitMultiple, bool applyInitialDamage, bool applyDOT)
    {
        _currentSkill = skill;
        _hitMultiple = hitMultiple;
        _hitEnemies.Clear();
        _isActive = true;
        _applyInitialDamage = applyInitialDamage;
        _applyDOT = applyDOT;

        StopAllCoroutines();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive || !_netObj.IsOwner || !other.transform.root.CompareTag("Enemy")) return;

        EnemyBase enemy = other.transform.root.GetComponent<EnemyBase>();
        if (_hitEnemies.Contains(enemy)) return;

        _hitEnemies.Add(enemy);

        // Base Damage
        if (_applyInitialDamage)
        {

            var (damage, isCritic) = _currentSkill.effectType == EffectType.PhysicDamage
                ? _playerCombat.CalculatePhysicalDamage(_currentSkill.minEffectValue, _currentSkill.maxEffectValue)
                : _playerCombat.CalculateMagicDamage(_currentSkill.minEffectValue, _currentSkill.maxEffectValue);

            enemy.TakeDamageRpc(damage, _currentSkill.effectType, isCritic, other.transform.position, _netObj);
            if (!_applyDOT) _isActive = false;
        }

        // Damage Over Time
        if (_applyDOT)
        {
            if (_currentSkill.minDamagePerSecond != 0)
                _playerCombat.StartCoroutine(ApplyDamageOverTime(_currentSkill, other, enemy));
        }

        if (!_hitMultiple)
        {
            _isActive = false;
        }
    }

    private IEnumerator ApplyDamageOverTime(Skill skill, Collider hit, EnemyBase enemy)
    {
        float elapsed = 0f;
        while (elapsed < skill.damagePerSecondTime)
        {
            yield return new WaitForSeconds(1f);
            elapsed += 1f;

            var (damage, isCritic) = skill.damagePerSecondType == EffectType.PhysicDamage
                ? _playerCombat.CalculatePhysicalDamage(skill.minDamagePerSecond, skill.maxDamagePerSecond)
                : _playerCombat.CalculateMagicDamage(skill.minDamagePerSecond, skill.maxDamagePerSecond);

            enemy.TakeDamageRpc(damage, skill.damagePerSecondType, isCritic, hit.transform.position, _netObj);
        }
        _isActive = false;
    }
}
