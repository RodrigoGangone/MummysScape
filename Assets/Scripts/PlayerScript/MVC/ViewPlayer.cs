using UnityEngine;

public class ViewPlayer
{
    Player _player;
    ModelPlayer _model;

    public GameObject ind;

    public ViewPlayer(Player p, ModelPlayer model)
    {
        _player = p;
        _model = model;

        ind = _player._indicatorShoot;
    }

    public void IndicatorAimOn(Vector3 hitposition) //
    {
            ind.transform.position = hitposition;
            ind.SetActive(true);
    }

    //TODO: PASAR TODOS LOS FEEDBACK A ESTE CODIGO (MUCHO MUY IMPORTANTE)
    
   // ───────▄▀▀▀▀▀▀▀▀▀▀▄▄
   // ────▄▀▀░░░░░░░░░░░░░▀▄
   // ──▄▀░░░░░░░░░░░░░░░░░░▀▄
   // ──█░░░░░░░░░░░░░░░░░░░░░▀▄
   // ─▐▌░░░░░░░░▄▄▄▄▄▄▄░░░░░░░▐▌
   // ─█░░░░░░░░░░░▄▄▄▄░░▀▀▀▀▀░░█
   // ▐▌░░░░░░░▀▀▀▀░░░░░▀▀▀▀▀░░░▐▌
   // █░░░░░░░░░▄▄▀▀▀▀▀░░░░▀▀▀▀▄░█
   // █░░░░░░░░░░░░░░░░▀░░░▐░░░░░▐▌
   // ▐▌░░░░░░░░░▐▀▀██▄░░░░░░▄▄▄░▐▌
   // ─█░░░░░░░░░░░▀▀▀░░░░░░▀▀██░░█
   // ─▐▌░░░░▄░░░░░░░░░░░░░▌░░░░░░█
   // ──▐▌░░▐░░░░░░░░░░░░░░▀▄░░░░░█
   // ───█░░░▌░░░░░░░░▐▀░░░░▄▀░░░▐▌
   // ───▐▌░░▀▄░░░░░░░░▀░▀░▀▀░░░▄▀
   // ───▐▌░░▐▀▄░░░░░░░░░░░░░░░░█
   // ───▐▌░░░▌░▀▄░░░░▀▀▀▀▀▀░░░█
   // ───█░░░▀░░░░▀▄░░░░░░░░░░▄▀
   // ──▐▌░░░░░░░░░░▀▄░░░░░░▄▀
   // ─▄▀░░░▄▀░░░░░░░░▀▀▀▀█▀
   // ▀░░░▄▀░░░░░░░░░░▀░░░▀▀▀▀▄▄▄▄▄
    
    //public void IndicatorShoot()
    //{
    //    if(_model != null && _model.Aim() != Vector3.zero)
    //    {
    //        Debug.Log("ENTRO PERO NO HIZO NADA, COMO ALLA");
    //        GameObject indicatorGameObject = Object.Instantiate(_indicatorShoot, _model.Aim(), Quaternion.identity);
    //        GameObject.Destroy(indicatorGameObject, 1f);
    //    }
    //    else
    //    {
    //        Debug.Log("DEBUG DE QUE DIO NULO");
    //    }
    //}
}
