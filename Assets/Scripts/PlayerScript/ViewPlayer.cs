using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ViewPlayer
{
    Player _player;
    ModelPlayer _model;
    public ViewPlayer(Player p, ModelPlayer model)
    {
        _player = p;
        _model = model;
    }
}
