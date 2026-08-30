using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Skills/SkillBehaviour")]
public abstract class SkillBehaviour : ScriptableObject
{
    public CastType castType;

    public abstract void Execute(GameObject caster, Skill skillData, Vector3 targetPosition);

    public virtual void OnHold(GameObject caster, Skill skillData, Vector3 targetPosition) { }

    public virtual void OnHoldUpdate(GameObject caster, Skill skillData, Vector3 targetPosition) { }
    
    public virtual bool OnRelease(GameObject caster, Skill skillData, Vector3 targetPosition) { return true; }
}