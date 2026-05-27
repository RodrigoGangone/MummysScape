using System;

public interface IStateRedirector
{
    Enum RedirectionCheck(Enum requestedState, Enum currentState);
    
}