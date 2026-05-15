using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SfxIDs;

public class SmashObject : MonoBehaviour
{
    [SerializeField] private GameObject _bubbles;
    [SerializeField] private bool _inWater;
    [SerializeField] private StandingTable _standingTable;

    [SerializeField] private Transform _wayPoint;
    [SerializeField] private List<GameObject> _tables;

    private const float _velocity = 3;

    [SerializeField] private MeshRenderer _fatherView;
    [SerializeField] private Collider _triggerCollider;
    [SerializeField] private Collider _fatherCollider;

    [SerializeField] private ParticleSystem _destroyFx;
    [SerializeField] private FxBank bank;
    
    // Variable para evitar que se rompa dos veces si le pegas muy rápido
    private bool _isBroken = false; 

    private void Start()
    {
        _bubbles.SetActive(_inWater);
        
        if(_inWater)
            bank.Play3D(SmashBox.Bubble, transform.position);

        _standingTable.Tables = _tables;
        _standingTable.ArrangeTables += () =>
        {
            _standingTable.replacementTables = StartCoroutine(MoveTablesWithDelay());
        };
    }

    // Ya no dependemos de OnTriggerEnter, pero creamos este método público
    public void DoBreak()
    {
        if (_isBroken) return; // Si ya se rompió, no hacemos nada
        _isBroken = true;

        _destroyFx.Play();
        bank.Play3D(SmashBox.Break, transform.position);
        _fatherView.enabled = false;
        _fatherCollider.enabled = false;
        _triggerCollider.enabled = false;

        _bubbles.GetComponent<ParticleSystem>().Stop();

        if (_inWater)
        {
            bank.Stop(SmashBox.Bubble);
            ActivateTables();
            StartCoroutine(MoveTablesWithDelay());
        }
    }
    
    private IEnumerator MoveTablesWithDelay()
    {
        for (int i = 0; i < _tables.Count; i++)
        {
            StartCoroutine(MoveToWaypoint(_tables[i], _wayPoint));
            yield return new WaitForSeconds(0.5f);
        }

        foreach (var table in _tables)
        {
            table.GetComponent<MeshCollider>().enabled = true;
        }
    }

    private void ActivateTables()
    {
        foreach (var table in _tables)
        {
            table.SetActive(true);
        }
    }

    private IEnumerator MoveToWaypoint(GameObject table, Transform targetPosition)
    {
        while (Vector3.Distance(table.transform.position, targetPosition.position) > 0.01f ||
               table.transform.rotation != targetPosition.rotation)
        {
            table.transform.position =
                Vector3.Lerp(table.transform.position, targetPosition.position, _velocity * Time.deltaTime);

            table.transform.rotation = Quaternion.Slerp(
                table.transform.rotation, targetPosition.rotation, _velocity * Time.deltaTime);
            yield return null;
        }

        table.transform.rotation = targetPosition.rotation;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (bank == null) return;

        // Visualizamos el rango de las burbujas (en Cyan)
        if (_inWater)
        {
            bank.DrawGizmo(transform.position, "Bubble", Color.cyan);
        }

        // Visualizamos el rango de la rotura (en Rojo)
        // Esto es útil para saber qué tan lejos se oirá el "CRASH"
        bank.DrawGizmo(transform.position, "Break", Color.red);
    }
}