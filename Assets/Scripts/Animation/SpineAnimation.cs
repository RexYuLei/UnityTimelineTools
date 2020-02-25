/************************************************************************************
 * 文件名：  SpineAnimation
 * 版本号：  V1.0.0.0
 * 创建人：LuoYu
 * 邮箱：  guyuelun545@126.com
 * 创建时间：2016/7/27 14:23:09
 * 描述：
 * 
 * 修改:
 * 
 ************************************************************************************/
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using UnityEngine;

public class SpineAnimation : MonoBehaviourEx
{
    public SkeletonAnimation _skAnima;
    public delegate void AnimationEventDelegate(string clipName, Spine.Event e);
    public AnimationEventDelegate Event;
    private List<SpineAniController.AniEventData> _skAniData;
    private List<SpineAniController.AniEventSubData> _curEventList;
    private List<SpineAniController.AniEventSubData>.Enumerator _EventEnumerator;
    private int _curFrameCount;
    private bool _needInit;
    private bool _forUI;
    private string _curAniName;
    private SpineAniController.AniEventSubData _curTickEvent;
    private Material m_CurMaterial;
    public bool InAlphaAni = false;
    public bool InHurtAni = false;
    public float CurAlpha = 1f;

    public Color ChangeColor;
    public int ChangeAniCount = 15;

    protected override void DoAwake()
    {
        base.DoAwake();
        _curEventList = new List<SpineAniController.AniEventSubData>();
    }

    public void Init(AnimationEventDelegate e)
    {
        _skAniData = SpineAniController.Instance.GetSpineAniData(_skAnima.name);
        Event = e;
        _curEventList.Clear();
        //if (_skAnima != null)
        //    _skAnima.state.Event += OnAnimaEvent;
    }

    public void SetForUI()
    {
        _forUI = true;
    }

    public void Clear()
    {
        Event = null;
        //if (_skAnima != null)
        //    _skAnima.state.Event -= OnAnimaEvent;
    }

    protected override bool CheckIsInit()
    {
        return base.CheckIsInit() && _skAnima != null;
    }

    //protected override void DoStart()
    //{
    //    base.DoStart();
    //}

    //protected override void DoEnable()
    //{
    //    base.DoEnable();
    //}

    //protected override void DoDisable()
    //{
    //    base.DoDisable();
    //}

    protected override void DoDestroy()
    {
        base.DoDestroy();
        _skAnima = null;
    }

    protected override void DoUpdate()
    {
        base.DoUpdate();
        if (_skAnima == null || _skAniData == null || _skAnima.state == null)
            return;
        if (_skAnima.state.ToString() == "<none>" || _curEventList == null)
            return;

        SetAlpha(CurAlpha);
        var track = _skAnima.state.GetCurrent(0);
        _curFrameCount = Mathf.FloorToInt(track.TrackTime * 30f);

        if ((_skAnima.loop && track.TrackTime > track.Animation.Duration) || _needInit)
        {
            // 若循环播放，则播完一轮后，事件重新加入队列
            track.TrackTime = 0;
            _curEventList.Clear();
            //@la 有问题这里改一下
            SpineAniController.AniEventData _curPlayAniData = new SpineAniController.AniEventData();
            var e = _skAniData.GetEnumerator();
            while (e.MoveNext())
            {
                if (e.Current.Name == _curAniName)
                {
                    _curPlayAniData = e.Current;
                }
            }
            if (_curPlayAniData.EventList != null && _curPlayAniData.EventList.Count != 0)
            {
                for (int index = 0; index < _curPlayAniData.EventList.Count; ++index)
                {
                    _curEventList.Add(_curPlayAniData.EventList[index]);
                }
            }
            _needInit = false;
            if (_curFrameCount == 0)
                return;
        }
        if (_curEventList.Count == 0)
            return;

        /*for (int i = 0; i < _curEventList.Count; i++)
        {
            var curTickEvent = _curEventList[i];
            if (curTickEvent.Check(_curFrameCount))
            {
                _curEventList[i] = curTickEvent;

                Spine.EventData eData = new Spine.EventData(curTickEvent.DataName);
                eData.String = curTickEvent.EventString;
                Spine.Event e = new Spine.Event(0f, eData);
                e.String = curTickEvent.EventString;
                if (_forUI && curTickEvent.DataName == "PlayAudio")
                {
                    // ignore
                }
                else
                    Event(_curAniName, e);
            }
        }*/

        _EventEnumerator = _curEventList.GetEnumerator();
        while (_EventEnumerator.MoveNext())
        {
            _curTickEvent = _EventEnumerator.Current;

            if (_curTickEvent.Check(_curFrameCount))
            {
                if (Event != null)
                {
                    Spine.EventData eData = new Spine.EventData(_curTickEvent.DataName);
                    eData.String = _curTickEvent.EventString;
                    Spine.Event e = new Spine.Event(0f, eData);
                    e.String = _curTickEvent.EventString;
                    if (_forUI && _curTickEvent.DataName == "PlayAudio")
                    {
                        // ignore
                    }
                    else
                        Event(_curAniName, e);

                }
                _curEventList.Remove(_EventEnumerator.Current);
                _EventEnumerator = _curEventList.GetEnumerator();
            }
        }
    }

    public void Play(string clipName, bool loop, float timeScale = 2)
    {
        if (_skAnima == null)
            return;
        _skAnima.loop = loop;
        _skAnima.timeScale = timeScale;
        _curAniName = clipName;
        _curFrameCount = -1;
        _needInit = true;
        _curEventList.Clear();
        if (_skAnima.state == null)
            _skAnima.Initialize(false);
        if (_skAnima.state == null)
        {
            Debug.LogError("This Spine Error : " + gameObject.name + "      ClipName :" + clipName);
            return;
        }
        _skAnima.state.SetAnimation(0, clipName, loop);

        var data = AniAudio.Instance.GetAudioData(_skAnima.name, clipName);
        if (data == null) return;
        var mAudio = data.Audio.Split(';');
        var mTime = data.Time.Split(';');
        var mVolume = data.Volume.Split(';');
        for (var i = 0; i < mAudio.Length; i++)
        {
            if (string.IsNullOrEmpty(mAudio[i])) continue;

            Action callback = delegate
            {
                if (string.Equals(clipName, _curAniName))
                {
                    // OldAudioMananger.AudioData audioData = new OldAudioMananger.AudioData();
                    // audioData.Type = OldAudioMananger.AudioType.SE;
                    // audioData.Path = mAudio[i];
                    // audioData.InTime = 0f;
                    // audioData.OutTime = 0f;
                    // audioData.Volume = float.Parse(mVolume[i]);
                    // OldAudioMananger.Instance.Play(audioData);
                    AudioManager.Instance.PlayBSE(mAudio[i] , float.Parse(mVolume[i]));
                }
            };
            // OldAudioMananger.Instance.PlayBySpineAni(float.Parse(mTime[i]), callback);
            GameUpdateMgr.Instance.CreateTimer(float.Parse(mTime[i]), callback);
        }
    }

    public void Stop()
    {
        _skAnima.timeScale = 0;
    }

    void OnAnimaEvent(Spine.AnimationState state, int trackIndex, Spine.Event e)
    {
        if (Event != null)
            Event(state.GetCurrent(trackIndex).Animation.Name, e);
    }

    public Spine.Slot GetSlot(string slotName)
    {
        var slot = _skAnima.skeleton.FindSlot(slotName);
        if (slot == null)
        {
            Debug.LogErrorFormat("not find slot '{0}' in {1}", slotName, name);
        }
        return slot;
    }

    public Spine.Bone GetBone(string boneName)
    {
        var bone = _skAnima.skeleton.FindBone(boneName);
        if (bone == null)
        {
            Debug.LogErrorFormat("not find slot '{0}' in {1}", boneName, name);
        }
        return bone;
    }

    public bool GetSlotPos(string slotName, out Vector3 pos)
    {
        pos = Vector3.zero;
        var slot = GetSlot(slotName);
        if (slot != null)
        {
            //这里并没有实际去获取slot的位置
            pos = transform.TransformPoint(new Vector3(slot.Bone.WorldX, slot.Bone.WorldScaleY, gameObject.transform.position.z));
            return true;
        }
        return false;
    }

    public bool GetBonePos(string boneName, out Vector3 pos)
    {
        pos = Vector3.zero;
        var bone = GetBone(boneName);
        if (bone != null)
        {
            pos = new Vector3(bone.WorldX, bone.WorldScaleY, gameObject.transform.position.z);
            return true;
        }
        return false;
    }

    private float m_CurTimeScale;
    private bool m_bInPause;
    public void Pause(float time)
    {
        if (!m_bInPause)
        {
            m_bInPause = true;
            m_CurTimeScale = _skAnima.timeScale;
            _skAnima.timeScale = 0f;
            Invoke("Recover", time);
        }
        else
        {
            CancelInvoke("Recover");
            Invoke("Recover", time);
        }
    }

    private Action m_Delay;
    public void Fast(float time, float delay, float timeScale)
    {
        Invoke("DelayToFast", delay);
        m_Delay = delegate
        {
            m_CurTimeScale = _skAnima.timeScale;
            _skAnima.timeScale = timeScale;
            Invoke("Recover", time);
        };
    }

    private void Recover()
    {
        m_bInPause = false;
        _skAnima.timeScale = 2f;
    }

    private void DelayToFast()
    {
        m_Delay.Invoke();
    }

    private void SetAlpha(float alpha)
    {
        if (!InAlphaAni)
            return;
        m_CurMaterial = GetComponentInChildren<Renderer>().material;
        m_CurMaterial.SetFloat("_TextureAlpha", alpha);
    }

    private Coroutine CurColorCorou;
    public void StartColorChange(string path, float time)
    {
        //if (CurColorCorou != null)
        //    StopCoroutine(CurColorCorou);
        //CurColorCorou = StartCoroutine(HurtColorChange(path, time));
    }

    private IEnumerator HurtColorChange(string path, float time)
    {
        yield return null;
        //GameObject obj = ResourceMgr.Instance.Load(path, null) as GameObject;
        //CurvesControl cc = obj.GetComponent<CurvesControl>();
        //CurvesFunction cf = new CurvesFunction(cc, time, 1f);

        //Color curColor = Color.white;
        //while (!cf.IsOver)
        //{
        //    yield return 0;
        //    cf.TickCurves(Time.deltaTime);
        //    curColor = cf.CurYValue * cc.AniColor;
        //    curColor.a = 1f;
        //    m_CurMaterial = GetComponentInChildren<Renderer>().material;
        //    m_CurMaterial.SetColor("_AddColor", curColor);
        //}

    }
}
