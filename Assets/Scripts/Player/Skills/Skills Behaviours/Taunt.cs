using Obrissom.Player;
using Obrissom.UI;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Behaviours/Taunt")]
public class Taunt : SkillBehaviour
{
    [SerializeField] private float _radius = 10f;
    [SerializeField] private float _tauntDuration = 5f;
    [SerializeField] private float _showCircleTime = 2f; // could be any other effect, also instead of a fixed time could be while the user holds the key
    private GameObject _tauntCircle;
    private PlayerCombat _playerCombat;

    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        if (_tauntCircle == null) _tauntCircle = TankUIManager.Instance.GetTauntCircle();
        if (_playerCombat == null) _playerCombat = caster.GetComponent<PlayerCombat>();
        _playerCombat.StartCoroutine(ShowTauntCircle());
        var hitbox = caster.GetComponentInChildren<TauntHitBox>(true);
        hitbox.SetupCollider(_radius, _tauntDuration);
    }

    private IEnumerator ShowTauntCircle()
    {
        _tauntCircle.SetActive(true);
        yield return new WaitForSecondsRealtime(_showCircleTime);
        _tauntCircle.SetActive(false);
    }
}
