using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorSpineMgr : GameMgrBase<ActorSpineMgr>
{
    private Dictionary<int, SkeletonAnimation> m_SpineContainer;
    private int m_CurID;

    public override bool Init(params object[] param)
    {
        m_SpineContainer = new Dictionary<int, SkeletonAnimation>();
        m_CurID = 1;
        return base.Init(param);
    }

    public int AddIn(SkeletonAnimation sa)
    {
        int id = m_CurID;
        m_SpineContainer.Add(id, sa);
        m_CurID++;
        return id;
    }

    public void DeleteOut(int id)
    {
        if (m_SpineContainer.ContainsKey(id))
        {
            m_SpineContainer.Remove(id);
        }
    }

    public void Pause(bool val)
    {
        foreach (var sa in m_SpineContainer.Values)
        {
            sa.timeScale = val ? 0f : 2f;
        }
    }

    public void Pause(float time)
    {
        Pause(true);
        StartCoroutine(Recover(time));
    }

    private IEnumerator Recover(float time)
    {
        yield return new WaitForSeconds(time);
        Pause(false);
    }

    public override void CleanUp()
    {
        m_SpineContainer.Clear();
    }
}
