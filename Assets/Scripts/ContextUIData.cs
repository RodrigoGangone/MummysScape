using UnityEngine;

[System.Serializable]
public struct ContextUIData
{
    public bool Visible;
    public bool UseButton;
    public buttonType Button;
    public ContextMessageType MessageType;
    public string CustomText;
    public Color TextColor;

    public ContextUIData(
        bool visible,
        bool useButton,
        buttonType button,
        ContextMessageType messageType,
        string customText,
        Color textColor)
    {
        Visible = visible;
        UseButton = useButton;
        Button = button;
        MessageType = messageType;
        CustomText = customText;
        TextColor = textColor;
    }
}

public static class ContextUIFactory
{
    /// <summary>
    /// Oculta completamente el UI de contexto.
    /// 
    /// USO:
    /// - Cuando el jugador sale de un trigger.
    /// - Cuando ya no hay nada relevante para mostrar.
    /// - Estado default / limpieza del UI.
    /// 
    /// Es el "reset" del sistema.
    /// </summary>
    public static ContextUIData Hidden()
        => new ContextUIData(
            visible: false,
            useButton: false,
            button: default,
            messageType: ContextMessageType.None,
            customText: string.Empty,
            textColor: Color.white
        );

    /// <summary>
    /// Muestra un prompt con botón (ej: "Presiona A para interactuar").
    /// 
    /// USO:
    /// - Interacciones del jugador (puertas, palancas, NPCs, etc).
    /// - Tutoriales interactivos donde el jugador debe presionar algo.
    /// 
    /// Usa un mensaje predefinido (ContextMessageType) + icono de botón.
    /// </summary>
    public static ContextUIData Prompt(ContextMessageType type, buttonType button, Color? color = null)
        => new ContextUIData(
            visible: true,
            useButton: true,
            button: button,
            messageType: type,
            customText: string.Empty,
            textColor: color ?? Color.white
        );

    /// <summary>
    /// Muestra un mensaje SIN botón.
    /// 
    /// USO:
    /// - Información pasiva (ej: "No puedes pasar", "Zona bloqueada").
    /// - Indicaciones de tutorial que no requieren input inmediato.
    /// 
    /// Ideal para feedback visual sin interacción directa.
    /// </summary>
    public static ContextUIData Message(ContextMessageType type, Color? color = null)
        => new ContextUIData(
            visible: true,
            useButton: false,
            button: default,
            messageType: type,
            customText: string.Empty,
            textColor: color ?? Color.white
        );

    /// <summary>
    /// Muestra un prompt con botón usando texto personalizado.
    /// 
    /// USO:
    /// - Casos especiales donde el mensaje no está en el enum.
    /// - Eventos dinámicos (ej: "Mantén RT para cargar", "Usa X habilidad").
    /// 
    /// Combina flexibilidad (texto libre) + input del jugador.
    /// </summary>
    public static ContextUIData CustomPrompt(string text, buttonType button, Color? color = null)
        => new ContextUIData(
            visible: true,
            useButton: true,
            button: button,
            messageType: ContextMessageType.Custom,
            customText: text,
            textColor: color ?? Color.white
        );

    /// <summary>
    /// Muestra un mensaje personalizado SIN botón.
    /// 
    /// USO:
    /// - Narrativa breve o contexto dinámico.
    /// - Feedback específico del gameplay (ej: "Necesitas más vendas").
    /// - Mensajes que no ameritan interacción.
    /// 
    /// Máxima flexibilidad para texto libre.
    /// </summary>
    public static ContextUIData CustomMessage(string text, Color? color = null)
        => new ContextUIData(
            visible: true,
            useButton: false,
            button: default,
            messageType: ContextMessageType.Custom,
            customText: text,
            textColor: color ?? Color.white
        );
}

public enum ContextMessageType
{
    None,
    Interact,
    Enter,
    ReplayTutorial,
    CancelReplay,
    Custom
}