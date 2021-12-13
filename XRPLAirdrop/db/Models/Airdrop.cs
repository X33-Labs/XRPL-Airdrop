using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XRPLAirdrop.db.models
{
    [Table("Airdrop")]
    public class Airdrop
    {
        [Column("id")]
        [Key]
        public int id { get; set; }

        [Column("address")]
        public string address { get; set; }

        [Column("airdropped")]
        public int dropped { get; set; }

        [Column("datetime")]
        public int datetime { get; set; }

        [Column("txn_message")]
        public string txn_message { get; set; }

        [Column("txn_detail")]
        public string txn_detail { get; set; }

        [Column("balance")]
        public decimal balance { get; set; }

        [Column("txn_hash")]
        public string txn_hash { get; set; }

        [Column("txn_verified")]
        public int txn_verified { get; set; }

        [Column("xrpl_verified")]
        public int xrpl_verified { get; set; }
    }
}



