void MainLight_half(out half3 Direction)  //half: vector de 3 componentes (x,y,z) de precision media
                                          //Utilizado para almacenar direcciones o colores.
{
   #if SHADERGRAPH_PREVIEW   //Si else preview está definido, else establece un vector que apunta hacia arriba
        Direction = half3(0,1,0); 
    #else
        Light light = GetMainLight(); //Se obtiene la dirección real de la luz principal en la escena. 
        Direction = light.direction;
   #endif
}