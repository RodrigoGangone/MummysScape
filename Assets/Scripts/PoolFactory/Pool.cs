using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class Pool<T>
//Generic Class, se puede crear una pool de lo que sea desde otro script reemplazando la T
{
    private Func<T> _factoryMethod;
    //Delegado que devuelve tipo T, por lo que aca voy a guardar el metodo de como se crea el objeto.

    private Action<T> _turnOnCallBack;
    //Delegado que toma por parámetro T, para guardar como se prende la bala una vez la llame el cliente.

    private Action<T> _turnOffCallBack;
    //Delegado que toma por parámetro T, para guardar como se apaga la bala una vez regrese al pool.

    List<T> _currentStock;
    //El cajon donde se guardan los objetos disponibles para su uso.


    //Se inicializa el pool
    public Pool(Func<T> factoryMethod, Action<T> turnOnCallBack, Action<T> turnOffCallBack, int initialAmount)
    {
        _currentStock = new List<T>(); //Se inicializa la lista

        _factoryMethod = factoryMethod;

        _turnOnCallBack = turnOnCallBack;

        _turnOffCallBack = turnOffCallBack;

        for (int i = 0; i < initialAmount; i++)
        {
            T newObj = _factoryMethod(); //En este caso, llama a CreateObject

            _turnOffCallBack(newObj);

            _currentStock.Add(newObj);
            //Se crea una cantidad de balas, se desactivam y se llena la lista con las mismas.
        }
    }

    //Se pide un objeto del pool
    public T GetObject() //Devuelve un objeto genérico
    {
        T result;

        if (_currentStock.Count == 0)
        {
            result = _factoryMethod(); //Si no hay más, se crea uno
        }

        else
        {
            result = _currentStock[0]; //Si hay, se agarra el primero de la lista.
            _currentStock.RemoveAt(0);
        }

        _turnOnCallBack(result); //Se pasa el objeto generico creado para ser activado en su script específico.
        return result;
    }

    //Vuelve el objeto al pool
    public void ReturnObject(T obj)
    {
        _turnOffCallBack(obj);
        _currentStock.Add(obj);
    }
}
