using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class QuickSandNEW : MonoBehaviour
{
    [Header("ATRIBUTES INVISIBLE")] //Plataforma invisible dentro de la arena
    private Vector3 _startPosInvisible; //Posicion a la que va a regresar la plat invisible
    private Vector3 _endPosInvisible;
    
    private Transform _invisiblePlatform; //Referencia de la plataforma invisible

    [SerializeField] private StatePlatform _state; //Direccion hacia donde tiene que ir la plataforma

    [Header("MOVE QUICKSAND")] //Al subir o bajar la Arena
    [SerializeField] private float yVariant;

    private Vector3 _startPosQuickSand; //Pos inicial
    private Vector3 _endPosQuickSand; //Se setea por hierarchy

    [Header("VARIABLES")] 
    [SerializeField] private float _speedSink; //Velocidad de hundision
    [SerializeField] private float _speedMove; //Velocidad de movision

    private bool isActivated; //Si se activo el metodo para mover la arena
    [SerializeField] private bool _onModifyQuickSand; //Verifico si el player esta en la arena movediza

    [SerializeField] private float _timeInvPlat;
    private float _timeActSand;


    void Awake()
    {
        //Posicion inicial/final de la arena (Visual)
        _startPosQuickSand = transform.position;
        _endPosQuickSand = new Vector3(_startPosQuickSand.x, yVariant, _startPosQuickSand.z);

        //Posicion inicial/final de la plat inv (Invisible)
        _startPosInvisible = transform.position;
        _endPosInvisible = new Vector3(_startPosInvisible.x, _startPosInvisible.y - 3, _startPosInvisible.z);
        
        //Referencia de plataforma invisible
        _invisiblePlatform = transform.GetChild(0);
    }
    
    void Update()
    {
        if (isActivated)
            ActivateSand();

        if (!_onModifyQuickSand) return;

        _invisiblePlatform.position = StateSand(_state, _invisiblePlatform.position);
    }

    public void ActivateSand()
    {
        isActivated = true;

        _timeActSand += Time.deltaTime * _speedMove;

        transform.position = Vector3.Lerp(_startPosQuickSand, _endPosQuickSand, _timeActSand);

        if (Vector3.Distance(transform.position, _endPosQuickSand) <= 0.1f)
        {
            isActivated = false;

            var newStartPos = _endPosQuickSand;
            var oldStartPos = _startPosQuickSand;

            _startPosQuickSand = newStartPos;
            _endPosQuickSand = oldStartPos;
            _timeActSand = 0;
        }
    }

    Vector3 StateSand(StatePlatform statePlatform, Vector3 currentInvPos)
    {
        _timeInvPlat += Time.deltaTime * _speedSink;
        
        if (currentInvPos.y.Equals(_endPosInvisible.y))
            Debug.Log("EL PLAYER MURIO"); //TODO: hacer action para que ejecute muerte de player

        if (statePlatform == StatePlatform.DownSand)
            return Vector3.MoveTowards(currentInvPos, _endPosInvisible, _timeInvPlat);

        if (Vector3.Distance(_invisiblePlatform.position, _startPosInvisible) < 0.01f)
            _onModifyQuickSand = false;

        return Vector3.MoveTowards(currentInvPos, _startPosInvisible, _timeInvPlat);
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

        var player = other.gameObject.GetComponent<Player>();

        if (player.CurrentPlayerSize == PlayerSize.Head)
        {
            _state = StatePlatform.UpSand;
            _speedSink = 0.001f; //Velocidad para subir
        }
        else
        {
            _onModifyQuickSand = true;
            _state = StatePlatform.DownSand;

            if (player.CurrentPlayerSize.Equals(PlayerSize.Normal)) //Velocidad para hundir
                _speedSink = 0.001f;
            else
                _speedSink = 0.0001f;
            
            other.transform.SetParent(_invisiblePlatform);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

        _onModifyQuickSand = true;
        _state = StatePlatform.DownSand;

        _timeInvPlat = 0;
        other.gameObject.GetComponent<Player>().ChangeSpeed();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

        _state = StatePlatform.UpSand;

        _timeInvPlat = 0;

        var player = other.gameObject.GetComponent<Player>();
        player.Speed = player.SpeedOriginal;
        other.transform.SetParent(null);

        StartCoroutine(ResetInvisiblePlatform()); //Delay al reposicionar invPlatform
    }

    private IEnumerator ResetInvisiblePlatform()
    {
        yield return new WaitForSeconds(0.2f);
        _invisiblePlatform.position = _startPosInvisible;
    }
}

enum StatePlatform
{
    DownSand,
    UpSand
}