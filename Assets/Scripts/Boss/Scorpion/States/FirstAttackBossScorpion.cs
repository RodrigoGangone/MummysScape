using System.Collections;
using UnityEngine;
using static Utils;

public class FirstAttackBossScorpion : State
{
    private FirstAttackProperties _firstAttackProperties;
    private Player _player;

    public FirstAttackBossScorpion(FirstAttackProperties firstAttackProperties)
    {
        _firstAttackProperties = firstAttackProperties;
    }

    public override void OnEnter()
    {
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
    }

    public IEnumerator MovePlayer(Vector3 dir)
    {
        float speed = 8f;
        Vector3 normalizedDir = dir.normalized;
        float rayLength = 1f; // Longitud del rayo para detectar la capa Sand

        while (_player._modelPlayer.CheckGround() && !IsOnSand(rayLength))
        {
            _player.transform.position += normalizedDir * (speed * Time.deltaTime);
            yield return null;
        }

        _player.GetComponent<Rigidbody>().AddForce(normalizedDir * (IsOnSand(rayLength) ? 10f : 5f), ForceMode.Impulse);

        yield return new WaitForSeconds(0.5f);

        _player._modelPlayer.IsDamage = false;
    }

    private bool IsOnSand(float rayLength)
    {
        Vector3 rayOrigin = _player.transform.position + Vector3.up * 0.1f; // Origen del rayo
        Ray ray = new Ray(rayOrigin, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayLength))
        {
            // Comprueba si la capa del objeto detectado es "Sand"
            return hit.collider.gameObject.layer == LayerMask.NameToLayer("Sand");
        }

        return false;
    }
}