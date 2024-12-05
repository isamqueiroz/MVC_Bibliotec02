using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

            if (form.Files.Count > 0)
            {
                //Primeiro passo
                //ARmazenaresmos o arquivo/foto enviado pelo meu usuario.
                var arquivo = form.Files[0];
                //Segundo passo
                //criar variavel do caminho da minha pasta para colocar as fotos do livro
                var pasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/Livros");
                //Validaresmos se a pasta que sera armazenada as imagens, existe. Caso nao exista, criaremos uma nova pasta.
                if (Directory.Exists(pasta))
                {
                    //Criar pasta:
                    Directory.CreateDirectory(pasta);

                    //terceiro passo:
                    //criar  a variavel para armazenar o caminho  em que meu arquivo estara, alem do nome dele.
                    var caminho = Path.Combine(pasta, arquivo.FileName);

                    using (var stream = new FileStream(caminho, FileMode.Create))
                    {
                        //Copiou o arquivo para o meu diretorio
                        arquivo.CopyTo(stream);
                    }
                }
                else
                {
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

        [Route("Editar/{id}")]
        public IActionResult Editar(int id)
        {

            ViewBag.Admin = HttpContext.Session.GetString("Admin")!;

            ViewBag.CategoriasDoSistema = context.Categoria.ToList();


            //Diario de um banana 
            //LivroID == 3

            //Buscar quem e o tal do Id numero 3
            Livro livroAtualizado = context.Livro.FirstOrDefault(Livro => Livro.LivroID == id)!;

            //Buscar as categorias que o livroAtualizado possui
            var categoriasDoLivroAtualizado = context.LivroCategoria.Where(identificadorLivro => identificadorLivro.LivroID == id).Select(Livro => Livro.Categoria).ToList();

            //Quero pegar as informacoes e mandar para a minha view 
            ViewBag.Livro = livroAtualizado;
            ViewBag.Categoria = categoriasDoLivroAtualizado;


            return View();
        }

        //metodo que atualiza as informacoes do livro 

        [Route("Atualizar/{id}")]
        public IActionResult Atualizar (IFormCollection form, int id, IFormFile imagem)
        {
            //BUScar um Livro especifico pelo ID
            Livro livroAtualizado = context.Livro.FirstOrDefault(livro => livro.LivroID == id)!;

            livroAtualizado.Nome = form["Nome"].ToString();
            livroAtualizado.Escritor = form["Escritor"].ToString();
            livroAtualizado.Editora = form["Editora"].ToString();
            livroAtualizado.Idioma = form["Idioma"].ToString();
            livroAtualizado.Descricao = form["Descricao"].ToString();

            if (imagem != null && imagem.Length > 0)
            {
                //Definir o caminho da minha imagem
                var caminhoImagem = Path.Combine("wwwroot/images/Livros", imagem.FileName);

                //verificar se minha imagem ainda existe no meu caminho.
                if (string.IsNullOrEmpty(livroAtualizado.Imagem))
                {
                    //caso exista ela ira ser excluida
                    var caminhoImagemAntiga = Path.Combine("wwwroot/images/Livros", livroAtualizado.Imagem);
                    //ver se existe uma imagem no caminho antigo.
                    if (System.IO.File.Exists(caminhoImagemAntiga))
                    {
                        System.IO.File.Delete(caminhoImagemAntiga);
                    }

                }

                using (var stream = new FileStream(caminhoImagem, FileMode.Create))
                {
                    imagem.CopyTo(stream);
                }

                //subir essa mudanca para o meu banco de dados
                livroAtualizado.Imagem = imagem.FileName;
            }

            //CATEGORIAS:

            //PRIMEIRO: Precisamos pegar as categorias selecionadas no usuario
            var categoriasSelecionadas = form["Categoria"].ToList();
            //SEGUNDO: Pegaremos as categorias ATUAIS do Livro
            var categoriasAtuais = context.LivroCategoria.Where(livro => livro.LivroID == id);
            //TERCEIRO: Removeremos as categorias antigas 
            foreach (var categoria in categoriasAtuais)
            {
                if (!categoriasSelecionadas.Contains(categoria.CategoriaID.ToString()))
                {
                    //nos vamos remover a categoria do nosso context
                    context.LivroCategoria.Remove(categoria);
                }
            }
            //QUARTO: Adicionaremos as novas categorias 
            foreach (var categoria in categoriasSelecionadas)
            {
                if (!categoriasAtuais.Any(c => c.CategoriaID.ToString() == categoria))
                {
                    context.LivroCategoria.Add(new LivroCategoria
                    {
                        LivroID = id,
                        CategoriaID = int.Parse(categoria)
                    });
                }

                context.SaveChanges();

            }
                return LocalRedirect("/Livro");
        }

       [Route("Excluir/{id}")]
       public IActionResult Excluir(int id){
        //Buscar qual o livro do id que precisamos excluir
        //Livro livroEncontrado = context.Livro.First(livro => livro.LivroID == id); 
        Livro livroEncontrado = context.Livro.First(livro => livro.LivroID == id);
        //BUscar  as categorias desse livro:
        var categoriasDoLivro = context.LivroCategoria.Where(livro => livro.LivroID == id).ToList();

        //precisamos excluir primeiro o registro da tabala intermediaria
        foreach (var categoria in categoriasDoLivro){
            context.LivroCategoria.Remove(categoria);
        }

        context.Livro.Remove(livroEncontrado);

        context.SaveChanges();
        


        return LocalRedirect("/Livro");
       }






    }

}


