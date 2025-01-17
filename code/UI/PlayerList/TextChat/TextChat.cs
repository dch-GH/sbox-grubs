namespace Grubs.UI;

public partial class TextChat : Panel
{
	public struct ChatMessage
	{
		public string Name;
		public Color Color;
		public string Message;
		public long SteamId;
	}

	private const int MaxItems = 100;
	private const float MessageLifetime = 10f;

	private Panel Canvas { get; set; }
	private TextEntry Input { get; set; }

	private readonly Queue<TextChatEntry> _entries = new();

	protected override void OnAfterTreeRender( bool firstTime )
	{
		base.OnAfterTreeRender( firstTime );

		Canvas.PreferScrollToBottom = true;
		Input.AcceptsFocus = true;
		Input.AllowEmojiReplace = true;
	}

	public override void Tick()
	{
		if ( Sandbox.Input.Pressed( InputAction.Chat ) && IsVisibleSelf )
			Open();
	}

	private void AddEntry( TextChatEntry entry )
	{
		Canvas.AddChild( entry );
		Canvas.TryScrollToBottom();

		entry.BindClass( "stale", () => entry.Lifetime > MessageLifetime );

		_entries.Enqueue( entry );
		if ( _entries.Count > MaxItems )
			_entries.Dequeue().Delete();
	}

	private void Open()
	{
		AddClass( "open" );
		Input.Focus();
		Canvas.TryScrollToBottom();
	}

	private void Close()
	{
		RemoveClass( "open" );
		Input.Blur();
		Input.Text = string.Empty;
		Input.Label.SetCaretPosition( 0 );
	}

	private void Submit()
	{
		var message = Input.Text.Trim();
		Input.Text = "";

		Close();

		if ( string.IsNullOrWhiteSpace( message ) )
			return;

		SendChat( message );
	}

	[ConCmd.Server( "gr_say" )]
	public static void SendChat( string message )
	{
		if ( !ConsoleSystem.Caller.IsValid() )
			return;

		if ( message.Contains( '\n' ) || message.Contains( '\r' ) )
			return;

		var color = (ConsoleSystem.Caller.Pawn as Player)?.Color ?? Color.White;
		AddChatEntry( To.Everyone, ConsoleSystem.Caller.Name, color, message, ConsoleSystem.Caller.SteamId );
	}

	[ClientRpc]
	public static void AddChatEntry( string name, Color color, string message, long steamId )
	{
		Event.Run( GrubsEvent.Player.ChatMessageSent, new ChatMessage() { Name = name, Color = color, Message = message, SteamId = steamId } );
	}

	[ClientRpc]
	public static void AddInfoChatEntry( string message )
	{
		Event.Run( GrubsEvent.Player.ChatMessageSent, new ChatMessage() { Message = message } );
	}

	[GrubsEvent.Player.ChatMessageSent]
	private void OnChatMessage( ChatMessage chatMessage )
	{
		AddEntry( new TextChatEntry { Name = chatMessage.Name, Color = chatMessage.Color, Message = chatMessage.Message, SteamId = chatMessage.SteamId } );
	}
}
