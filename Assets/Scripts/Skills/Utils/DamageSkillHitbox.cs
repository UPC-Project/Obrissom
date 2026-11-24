using UnityEngine;
using Obrissom.Enemy;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
using Obrissom.Player;

[RequireComponent(typeof(Collider))]
public class DamageSkillHitbox : MonoBehaviour
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
    private GameObject _caster;
    private bool _lockMovement = false;

    private void Awake()
    {
        _playerCombat = GetComponentInParent<PlayerCombat>();
        _netObj = GetComponentInParent<NetworkObject>();
    }

    public void SetupCollider(Skill skill, bool hitMultiple, bool applyInitialDamage, bool applyDOT, GameObject caster, bool lockMovement)
    {
        _currentSkill = skill;
        _hitMultiple = hitMultiple;
        _hitEnemies.Clear();
        _isActive = true;
        _applyInitialDamage = applyInitialDamage;
        _applyDOT = applyDOT;
        _caster = caster;
        _lockMovement = lockMovement;

        if (_lockMovement)
        {
            StartCoroutine(LockMovementCor());
        }

    }

    private IEnumerator LockMovementCor()
    {

        PlayerLocomotionInput locomotionInput = _caster.GetComponent<PlayerLocomotionInput>();
        PlayerCombatInput combatInput = _caster.GetComponent<PlayerCombatInput>();
        PlayerController playerController = _caster.GetComponent<PlayerController>();
        locomotionInput.enabled = false;
        combatInput.enabled = false;
        playerController.enabled = false;
        _caster.GetComponent<CharacterController>().enabled = false;

        yield return new WaitForSeconds(_currentSkill.damagePerSecondTime);

        locomotionInput.enabled = true;
        combatInput.enabled = true;
        playerController.enabled = true;
        _caster.GetComponent<CharacterController>().enabled = true;
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
            _playerCombat.StartCoroutine(ApplyDamageOverTime(_currentSkill, other, enemy));

        if (!_hitMultiple)
            _isActive = false;
    }

    private IEnumerator ApplyDamageOverTime(Skill skill, Collider hit, EnemyBase enemy)
    {
        Vector3 hitpos = hit.transform.position;
        float elapsed = 0f;
        while (elapsed < skill.damagePerSecondTime)
        {
            yield return new WaitForSeconds(1f);
            elapsed += 1f;
            if (!enemy.IsSpawned || enemy.IsDeadSynced) yield break;

            var (damage, isCritic) = skill.damagePerSecondType == EffectType.PhysicDamage
                ? _playerCombat.CalculatePhysicalDamage(skill.minDamagePerSecond, skill.maxDamagePerSecond)
                : _playerCombat.CalculateMagicDamage(skill.minDamagePerSecond, skill.maxDamagePerSecond);

            enemy.TakeDamageRpc(damage, skill.damagePerSecondType, isCritic, hitpos, _netObj);
        }
        
        _isActive = false;
    }
}
