namespace Usuarios.Api.Data
{
    public class StoredProcedureResult
    {
        public ResultCode ResultCode { get; set; }
        public string ResultMessage { get; set; }

        public static StoredProcedureResult Of(int rawResultCode, string message)
        {
            return new StoredProcedureResult
            {
                ResultCode = System.Enum.IsDefined(typeof(ResultCode), rawResultCode)
                    ? (ResultCode)rawResultCode
                    : ResultCode.ErrorEjecucion,
                ResultMessage = message
            };
        }
    }
}
