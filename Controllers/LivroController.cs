using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bibliotec.Contexts;
using Bibliotec.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bibliotec_mvc.Controllers
{
    [Route("[controller]")]
    public class LivroController : Controller
    {
        private readonly ILogger<LivroController> _logger;

        public LivroController(ILogger<LivroController> logger)
        {
            _logger = logger;
        }

        Context context = new Context();

        public IActionResult Index()
        {
            ViewBag.Admin = HttpContext.Session.GetString("Admin")!;

            //Criar uma lista de livros
            List<Livro> listaLivros = context.Livro.ToList();

            //Verificar se o livro tem reserva ou não
            //ToDictionay(chave, valor)
            var livrosReservados = context.LivroReserva.ToDictionary(livro => livro.LivroID, livror => livror.DtReserva);

            ViewBag.Livros = listaLivros;
            ViewBag.LivrosComReserva = livrosReservados;

            return View();
        }

        [Route("Cadastro")]
        //Método que retorna a tela de cadastro:
        public IActionResult Cadastro()
        {

            ViewBag.Admin = HttpContext.Session.GetString("Admin")!;

            ViewBag.Categorias = context.Categoria.ToList();
            //Retorna a View de cadastro:
            return View();
        }

        [Route("Cadastrar")]

        public IActionResult Cadastrar(IFormCollection form)
        {

            Livro novoLivro = new Livro();

            novoLivro.Nome = form["Nome"].ToString();
            novoLivro.Descricao = form["Descricao"].ToString();
            novoLivro.Editora = form["Editor"].ToString();
            novoLivro.Escritor = form["Escritor"].ToString();
            novoLivro.Idioma = form["Idioma"].ToString();

            if(form.Files.Count > 0){
                //Primeiro passo
                //ARmazenaresmos o arquivo/foto enviado pelo meu usuario.
                var arquivo = form.Files[0];
                 //Segundo passo
                //criar variavel do caminho da minha pasta para colocar as fotos do livro
                var pasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/Livros");
                //Validaresmos se a pasta que sera armazenada as imagens, existe. Caso nao exista, criaremos uma nova pasta.
                if(Directory.Exists(pasta)){
                    //Criar pasta:
                    Directory.CreateDirectory(pasta);

                //terceiro passo:
                //criar  a variavel para armazenar o caminho  em que meu arquivo estara, alem do nome dele.
                var caminho = Path.Combine(pasta, arquivo.FileName);
                
                using (var stream = new FileStream(caminho, FileMode.Create)){
                    //Copiou o arquivo para o meu diretorio
                    arquivo.CopyTo(stream);
                }
                }else {
                    novoLivro.Imagem = "padrao.png";
                }


                novoLivro.Imagem = arquivo.FileName;

            }

            context.Livro.Add(novoLivro);

            context.SaveChanges();


            List<LivroCategoria> ListaLivroCategorias = new List<LivroCategoria>();

            string[] categoriasSelecionada = form["Categoria"].ToString().Split(",");

            foreach (string categoria in categoriasSelecionada)
            {
                LivroCategoria livroCategoria = new LivroCategoria();

                livroCategoria.CategoriaID = int.Parse(categoria);
                livroCategoria.LivroID = novoLivro.LivroID;

                ListaLivroCategorias.Add(livroCategoria);

            }

            context.LivroCategoria.AddRange(ListaLivroCategorias);
            context.SaveChanges();


            return LocalRedirect("/Livro/Cadastro");
        }
    }
}