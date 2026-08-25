using System;

namespace Examenes.Dominio
{
	// LÁPIDA. Esta clase ya no hace nada y no la llama nadie. Existe SOLO para que los
	// journals escritos antes de 2026-08-25 se puedan seguir reproduciendo.
	//
	// POR QUE NO SE BORRA.
	//
	// Los permisos de la IA se fueron: quien se conecta con la IA es el Gnothi Host, no
	// esta app, asi que decidir que puede consultar no era responsabilidad nuestra. Habia
	// un malentendido de responsabilidades y esto era su consecuencia.
	//
	// Pero el journal es un log append-only, y el pasado no se edita. Los expedientes que
	// ya existen tienen literalmente esto adentro:
	//
	//     upgrade('permisos-de-la-ia')
	//     permisos = PermisosDeLaIA();
	//
	// -verificado en un journal real de esta maquina-, y el replay lo vuelve a ejecutar
	// tal cual. Si el tipo no existe, ese expediente deja de abrir. No seria una refactor:
	// seria romper el registro de alguien que ya lo tenia.
	//
	// Asi que el global se sigue creando al reproducir, no lo lee nadie, y se pierde al
	// terminar. Es basura inerte y esta bien que lo sea.
	//
	// CUANDO SE PUEDE BORRAR DE VERDAD: cuando el equipo confirme que no queda ningun
	// journal con esa entrada. Como los del demo se regeneran solos desde URL2 -basta
	// borrar la carpeta y apretar Refresh-, el dia que alguien quiera limpiar esto el
	// costo es una linea de aviso, no una migracion.
	//
	// Los metodos aceptan cualquier texto y no hacen nada a proposito: un journal podria
	// traer tambien un `permisos.Denegar('velocidad')` de cuando la pantalla existia, y
	// tiene que poder reproducirse igual. Por eso tampoco valida contra ningun conjunto
	// cerrado: ConsultaDeLaIA se borro, y hacer que esto dependa de el lo habria revivido.
	public class PermisosDeLaIA
	{
		public void Denegar(string consulta)
		{
		}

		public void Permitir(string consulta)
		{
		}
	}
}
