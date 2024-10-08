using System;
using System.Collections.Generic;

[Serializable]
public class Level
{
    public int level;
    public List<CollectibleNumber> collectibleNumbers;
    public float timeToCompleteLevel;
}
