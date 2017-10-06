﻿using Android.App;
using Android.Widget;
using Android.OS;
using System;

namespace ParkingApp
{
    [Activity(Label = "ParkingApp", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.TelaLogin);
            var btnCadastro = FindViewById<Button>(Resource.Id.buttonCadastro);
            var btnLogin = FindViewById<Button>(Resource.Id.buttonLogin);
            btnLogin.Click += capturaClickLogin;
            btnCadastro.Click += capturaClickCadastro;


        }

        private void capturaClickLogin(object sender, EventArgs e)
        {
            var editLogin = FindViewById<EditText>(Resource.Id.editEmail);
            var editSenha = FindViewById<EditText>(Resource.Id.editSenha);

            if(editLogin.Text == "Luccas" && editSenha.Text == "200996")
            {
                StartActivity(typeof(AtividadeMapa));
            }
            else
            {
                Android.App.AlertDialog.Builder dialog = new AlertDialog.Builder(this);
                AlertDialog alert = dialog.Create();
                alert.SetTitle("Erro");
                alert.SetMessage("Usuário ou senha incorretos");
                alert.SetButton("OK", (c, ev) =>
                {
                    // Ok button click task  
                });

                alert.Show();
            }
        }

        private void capturaClickCadastro(object sender, EventArgs e)
        {
            StartActivity(typeof(AtividadeCadastro));
        }
    }
}

