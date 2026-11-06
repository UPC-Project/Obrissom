using UnityEngine;

public abstract class SkillBehaviour : ScriptableObject
{
    public CastType castType;

    public abstract void Execute(GameObject caster, Skill skillData, Vector3 targetPosition);

    public virtual void OnHold(GameObject caster, Skill skillData, Vector3 targetPosition) { }

    public virtual void OnHoldUpdate(GameObject caster, Skill skillData, Vector3 targetPosition) { }
    
    public virtual bool OnRelease(GameObject caster, Skill skillData, Vector3 targetPosition) { return true; }

    public virtual void OnCancel(GameObject caster, Skill skillData) { }

    public virtual bool OnChannelStart(GameObject caster, Skill skillData) { return true; }

    public virtual bool OnChannelUpdate(GameObject caster, Skill skillData) { return true; }

    public virtual void OnChannelEnd(GameObject caster, Skill skillData) { }
}
