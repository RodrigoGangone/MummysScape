using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class QuickSandNEW : MonoBehaviour
{
    [Header("ATRIBUTES INVISIBLE")]
    private Vector3 _startPosInvisible; //Posicion a la que va a regresar la plat invisible

    private Transform _invisiblePlatform; //Referencia de la plataforma invisible

    private StatePlatform _state; //Direccion hacia donde tiene que ir la plataforma

    [Header("MOVE QUICKSAND")] [SerializeField]
    private float yVariant;

    private Vector3 _startPosQuickSand; //Pos inicial
    private Vector3 _endPosQuickSand; //Se setea por hierarchy

    [Header("VARIABLES")] [SerializeField] private float _speedSink; //Velocidad de hundision
    [SerializeField] private float _speedMove; //Velocidad de movision

    private bool isActivated; //Si se activo el metodo para mover la arena
    private bool _onModifyQuickSand; //Verifico si el player esta en la arena movediza

    private float _timeInvPlat;
    private float _timeActSand;


    void Awake()
    {
        //Posicion inicial de la arena (visual e invisible)
        _startPosQuickSand = transform.position;
        _endPosQuickSand = new Vector3(_startPosQuickSand.x, yVariant, _startPosQuickSand.z);

        //La posicion inicial a donde debe volver la plat invisible
        _startPosInvisible = transform.position;
        //Referencia de plataforma invisible
        _invisiblePlatform = transform.GetChild(0);
    }

    //TODO: MODIFICAR PARA QUE NO DESAPAREZCA EL COLLIDER, SINO QUE MATE AL JUGADOR
    //TODO: BARAJAR LA IDEA DE QUE YA QUE EXISTE UN STARPOSTINVISIBLE, QUE HAYA UN ENDPOSINVISIBLE, ENTONCES MANEJARIAMOS DOS VECTOR3.LERP
    //TODO: SEGUIR MEJORANDO LOGICA PARA QUE _onModifyQuickSand PASE A FALSE, ACTUALMENTE FUNCIONA PERO NO ES LO IDEAL

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

    Vector3 StateSand(StatePlatform statePlatform, Vector3 inv)
    {
        var heightDescend = -3;

        _timeInvPlat += Time.deltaTime * _speedSink;

        _invisiblePlatform.GetComponent<Collider>().enabled = _invisiblePlatform.position.y != heightDescend;

        if (statePlatform == StatePlatform.DownSand)
        {
            var endPosInvisiblePlatform = new Vector3(_startPosInvisible.x, heightDescend, _startPosInvisible.z);

            return Vector3.MoveTowards(inv, endPosInvisiblePlatform, _timeInvPlat);
        }

        if (Vector3.Distance(_invisiblePlatform.position, _startPosInvisible) < 0.01f)
            _onModifyQuickSand = false;

        return Vector3.MoveTowards(inv, _startPosInvisible, _timeInvPlat);
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

        var player = other.gameObject.GetComponent<Player>();

        if (player.CurrentPlayerSize == PlayerSize.Head)
        {
            _state = StatePlatform.UpSand;
            other.transform.SetParent(null);
        }
        else
        {
            _state = StatePlatform.DownSand;
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

        StartCoroutine(ResetInvisiblePlatform()); //Restaurar posicion al salir de la arena de forma casi instantanea
    }

    private IEnumerator
        ResetInvisiblePlatform() // Se hizo esto para que no te pegue el invisible platform en los pies de golpe al salir de la arena
    {
        var distanceToStartPos = Vector3.Distance(_invisiblePlatform.position, _startPosInvisible);
        float timeToReturn;

        switch (distanceToStartPos)
        {
            case <= 0.5f:
                timeToReturn = 0.2f;
                break;
            case > 0.5f:
                timeToReturn = 0.3f;
                break;
            default:
                timeToReturn = 0.2f;
                break;
        }

        yield return new WaitForSeconds(timeToReturn);
        _invisiblePlatform.position = _startPosInvisible;
    }
}

enum StatePlatform
{
    DownSand,
    UpSand
}