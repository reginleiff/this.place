﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private GameObject _player;
    public float AttentionRadius;
    public float IdleTime;
    public BoxCollider TopCollider;


    private EnemyController _enemyController;
    private EnemyState _state = EnemyState.Idle;
    private Animator _animator;
    private BlockFace _face;
    // Use this for initialization
    void Awake()
    {
        _animator = GetComponent<Animator>();
        _enemyController = GetComponentInParent<EnemyController>();
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        switch (_state)
        {
            case EnemyState.Asleep:
                CheckIfPlayerIsClose();
                break;
            case EnemyState.Dead:
            case EnemyState.Waiting:
            case EnemyState.Moving:
                break;
            case EnemyState.Idle:
                StartCoroutine(WaitThenMove());
                break;
        }
    }

    public void EnemyDeath()
    {
        if (_state == EnemyState.Idle || _state == EnemyState.Waiting)
        {
            GetComponent<BlockBehaviour>().enabled = true;
            // ensure does not eat any RayCasts
            TopCollider.enabled = false;
            _state = EnemyState.Dead;
        }
    }

    private void CheckIfPlayerIsClose()
    {
        Vector3 enemyToPlayer = _player.transform.position - transform.position;
        if (enemyToPlayer.magnitude < AttentionRadius)
        {
            MoveEnemy(enemyToPlayer);
        }
    }

    private IEnumerator WaitThenMove()
    {
        _state = EnemyState.Waiting;
        yield return new WaitForSeconds(IdleTime);

        if (_state == EnemyState.Dead) yield break;

        Vector3 enemyToPlayer = _player.transform.position - transform.position;
        if (enemyToPlayer.magnitude >= AttentionRadius)
        {
            _state = EnemyState.Asleep;
            yield break;
        }

        MoveEnemy(enemyToPlayer);
    }

    private void MoveEnemy(Vector3 enemyToPlayer)
    {
        _state = EnemyState.Moving;
        Vector3 dir = Mathf.Abs(enemyToPlayer.x) > Mathf.Abs(enemyToPlayer.z) ?
            Vector3.right * Mathf.Sign(enemyToPlayer.x) :
            Vector3.forward * Mathf.Sign(enemyToPlayer.z);
        BlockFace face = BlockFaceMethods.BlockFaceFromNormal(dir.normalized);
        _enemyController.LookAtFaceDir(face);
        _face = face;
        _animator.SetTrigger("Bounce");
    }

    // Animation Events found in Bounce
    private void OnBounceStart()
    {
        _enemyController.MoveEnemy(_face);
    }

    private void OnBounceEnd()
    {
        _state = EnemyState.Idle;
    }

    private enum EnemyState
    {
        Asleep,
        Dead,
        Idle,
        Moving,
        Waiting
    }
}
