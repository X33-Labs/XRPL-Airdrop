using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XRPLAirdrop.db.models
{
    [Table("ExclusionList")]
    public class ExclusionList
    {
        [Column("id")]
        [Key]
        public int id { get; set; }

        [Column("address")]
        public string address { get; set; }

        [Column("type")]
        public string type { get; set; }
    }
}
