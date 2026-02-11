         /// <summary>
      /// Should be thrown if adding a tool with an existing name
      /// </summary>
      public class ToolAlreadyExistsException : Exception
     {
         public ToolAlreadyExistsException() { }
         public ToolAlreadyExistsException(string message) : base(message) { }
     }

     /// <summary>
     /// Should be thrown if a tool is not found and we need it.
     /// </summary>
     public class ToolNotFoundException: Exception
      {
         public ToolNotFoundException() { }
         public ToolNotFoundException(string message) : base(message) { }
     }