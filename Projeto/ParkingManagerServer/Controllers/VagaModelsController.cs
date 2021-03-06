﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using ParkingManagerServer.Models;

namespace ParkingManagerServer.Controllers
{
    public class VagaModelsController : ApiController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: api/VagaModels
        public IQueryable<VagaModel> GetVagaModels()
        {
             var lista = db.VagaModels.ToList();
            return lista.AsQueryable();
        }

        // GET: api/VagaModels/5
        [ResponseType(typeof(VagaModel))]
        public IHttpActionResult GetVagaModel(long id)
        {
            VagaModel vagaModel = db.VagaModels.Find(id);
            if (vagaModel == null)
            {
                return NotFound();
            }

            return Ok(vagaModel);
        }


        // GET: api/VagaModels/{id}
        [HttpGet,Route("api/VagaModels/{id}/ModificarEstado/{estado}")]
        [ResponseType(typeof(VagaModel))]
        public IHttpActionResult ModificarEstado(long id, EstadoVaga estado)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var vaga = db.VagaModels.Find(id);


            if (estado == EstadoVaga.Ocupada)
            {
                if (vaga.Ocupacao == null)
                {
                    if (vaga.Reserva == null)
                    {
                        vaga.Ocupacao = new OcupacaoModel(0, null, null, DateTime.Now, DateTime.MaxValue);
                        db.Entry(vaga).State = EntityState.Modified;
                    }
                    else
                    if (vaga.Reserva != null)
                    {
                        vaga.Ocupacao = new OcupacaoModel(0, null, vaga.Reserva.Usuario, DateTime.Now, DateTime.MaxValue);
                        vaga.Reserva = null;
                        db.Entry(vaga).State = EntityState.Modified;
                    }
                    
                }


            }
            else
            {
                if (vaga.Ocupacao != null)
                {
                    vaga.Ocupacao.DataSaida = DateTime.Now;
                    db.Entry(vaga.Ocupacao).State = EntityState.Modified;
                    vaga.Ocupacao = null;
                    db.Entry(vaga).State = EntityState.Modified;

                }
            }
            db.SaveChanges();
            return Ok(vaga);
        }

        // GET: api/VagaModels/{id}
        [HttpGet,Route("api/VagaModels/{id}/Reservar/{idUsuario}")]
        public IHttpActionResult ModificarReserva(long id, long idUsuario )
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            ReservaModel reserva = new ReservaModel();

            UsuarioModel usuario = db.UsuarioModels.Find(idUsuario);
            if (usuario == null) return BadRequest("Usuário desconhecido.");

            reserva.Usuario = usuario;
            reserva.Data = DateTime.Now;
            reserva.DataEntrada = DateTime.Now.AddMinutes(15);
            reserva.DataSaida = DateTime.Now.AddHours(2);
            reserva.DataExpiracao = DateTime.Now.AddHours(3);



            var vaga = db.VagaModels.Find(id);


            if (id != vaga.Id)
            {
                return BadRequest();
            }

            if (vaga != null)
            {
                if (vaga.Reserva == null)
                {
                    vaga.Reserva = reserva;
                    db.Entry(vaga).State = EntityState.Modified;

                    try
                    {
                        db.SaveChanges();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!VagaModelExists(id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                else
                {
                    return BadRequest("A vaga já está reservada.");
                }
            }
            else
            {
                return BadRequest("A vaga não foi encontrada.");
            }

            return Ok(vaga);
        }


        // GET: api/VagaModels/{id}
        [HttpGet,Route("api/VagaModels/{id}/ModificarTipo")]
        public IHttpActionResult ModificarTipo(long id, TipoVaga tipoVaga)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            var vaga = db.VagaModels.Find(id);


            if (id != vaga.Id)
            {
                return BadRequest();
            }

            if (vaga != null)
            {
                
                    vaga.Tipo = tipoVaga;
                    db.Entry(vaga).State = EntityState.Modified;

                    try
                    {
                        db.SaveChanges();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!VagaModelExists(id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                
            }
            else
            {
                return BadRequest("A vaga não foi encontrada.");
            }

            return Ok(vaga);
        }



        // GET: api/VagaModels/{id}
        [Route("api/VagaModels/{id}/ModificarOcupacao")]
        public IHttpActionResult ModificarOcupacao(long id, OcupacaoModel ocupacao)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            var vaga = db.VagaModels.Find(id);


            if (id != vaga.Id)
            {
                return BadRequest();
            }

            if (vaga != null)
            {
                if (vaga.Ocupacao != null)
                {
                    vaga.Ocupacao = ocupacao;
                    db.Entry(vaga).State = EntityState.Modified;

                    try
                    {
                        db.SaveChanges();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!VagaModelExists(id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                else
                {
                    return BadRequest("A vaga ainda está ocupada.");
                }
            }
            else
            {
               return BadRequest("A vaga não foi encontrada.");
            }
                
            return Ok(vaga);
        }

               // POST: api/VagaModels
        [ResponseType(typeof(VagaModel))]
        public IHttpActionResult PostVagaModel(long idPonto,VagaModel vagaModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var ponto = db.PontoModels.Find(idPonto);
           
            if (ponto.VagasConectadas == null)
            {
                ponto.VagasConectadas = new List<VagaModel>();
            }
            ponto.VagasConectadas.Add(vagaModel);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = vagaModel.Id }, vagaModel);
        }

        // DELETE: api/VagaModels/5
        [ResponseType(typeof(VagaModel))]
        public IHttpActionResult DeleteVagaModel(long id)
        {
            VagaModel vagaModel = db.VagaModels.Find(id);
            if (vagaModel == null)
            {
                return NotFound();
            }

            db.VagaModels.Remove(vagaModel);
            db.SaveChanges();

            return Ok(vagaModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool VagaModelExists(long id)
        {
            return db.VagaModels.Count(e => e.Id == id) > 0;
        }
    }
}