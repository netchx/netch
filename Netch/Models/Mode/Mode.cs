namespace Netch.Models.Mode
{
    public class Mode
    {
        /// <summary>
        ///     类型
        /// </summary>
        [Newtonsoft.Json.JsonProperty("type")]
        public ModeType Type;

        /// <summary>
        ///     备注
        /// </summary>
        [Newtonsoft.Json.JsonProperty("remark")]
        public string Remark;

        public override string ToString() => $"[{((int)this.Type) + 1}] {this.Remark}";
    }
}
