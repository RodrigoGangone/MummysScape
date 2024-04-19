using UnityEngine;

public class ViewPlayer
{
    Player _player;
    ModelPlayer _model;
    GameObject _indicatorShoot;
    public ViewPlayer(Player p, ModelPlayer model, GameObject indicatorShoot)
    {
        _player = p;
        _model = model;
        _indicatorShoot = indicatorShoot;
    }

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
