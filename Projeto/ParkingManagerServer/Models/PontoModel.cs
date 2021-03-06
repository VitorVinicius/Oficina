﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ParkingManagerServer.Models
{
    public class PontoModel
    {
        [Key]
        public long Id { get; set; }
        public PosicaoGeografica Localizacao { get; set; }
        public virtual ICollection<VagaModel> VagasConectadas { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public virtual ICollection<PontoModel> PontosFilhosConectados { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public virtual ICollection<PontoModel> PontosPaisConectados { get; set; }
        public ICollection<long> Conexoes {
            get
            {
                List<long> conexoes = new List<long>();
                try
                {
                    foreach (var conexao in PontosFilhosConectados)
                    {
                        conexoes.Add(conexao.Id);
                    }
                }
                catch { }
                return conexoes;
            }
        }

        public ICollection<long> ConexoesComplexas
        {
            get
            {
                List<long> conexoes = new List<long>();
                try
                {
                    foreach (var conexao in PontosFilhosConectados)
                    {
                        conexoes.Add(conexao.Id);
                    }
                    foreach (var conexao in PontosPaisConectados)
                    {
                        conexoes.Add(conexao.Id);
                    }
                }
                catch { }
                return conexoes;
            }
        }

        public bool Entrada { get; set; }
        public bool Saida { get; set; }
    }
}