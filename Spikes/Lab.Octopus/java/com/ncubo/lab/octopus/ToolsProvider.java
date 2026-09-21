package com.ncubo.lab.octopus;

import android.content.ContentProvider;
import android.content.ContentValues;
import android.database.Cursor;
import android.database.MatrixCursor;
import android.net.Uri;
import android.os.Binder;
import android.util.Log;

/**
 * EL DESCUBRIMIENTO — la mitad de arriba del protocolo "octopus" de Claude Android.
 *
 * Contrato reconstruido del APK de produccion (ver NotasGnothiHost/20):
 *
 *   packageManager.queryIntentContentProviders(Intent("…tool.DISCOVER_TOOLS"))
 *   contentResolver.query(Uri.parse("content://" + authority + "/tools"), null,null,null,null)
 *   cursor.moveToFirst(); cursor.getString(cursor.getColumnIndex("tools"))
 *
 * UNA fila, UNA columna llamada "tools", con un array JSON de tools.
 *
 * ESTO NO ES UNA API PUBLICADA. Es ingenieria inversa de un binario. Puede cambiar sin aviso.
 *
 * El catalogo va escrito a mano porque el IpcService de Gnothi hoy solo sabe SERVIR, no
 * LISTAR — y no queriamos tocar Gnothi.Mobile para que este spike sea reversible con un
 * uninstall. La version de producto proyectaria ToolIndex, que ya produce este mismo
 * JSON Schema.
 */
public final class ToolsProvider extends ContentProvider {

    private static final String TAG = "OCTOPUS";

    /**
     * Las tres tools SIN ARGUMENTOS de los tres modulos que hay en el telefono. Se eligieron
     * sin argumentos a proposito: el IpcService pasa Request(module, tool, null) y todavia no
     * reenvia parametros. Con esto igual se prueba el cruce de modulos, que es lo que importa.
     */
    private static final String TOOLS_JSON = """
[
  {
    "name": "facturas_total_de_facturas",
    "title": "Total de facturas",
    "description": "Cuanto gasto en total la persona, sumando todas las facturas de compra que su app registro en la carpeta de ella en este telefono. Devuelve tambien cuantas facturas son. El dato sale del registro propio de la persona, no de ningun servidor.",
    "input_schema": { "type": "object", "properties": {}, "required": [] }
  },
  {
    "name": "compras_total_de_compras",
    "title": "Total de compras del supermercado",
    "description": "Cuanto gasto en total la persona en el supermercado, sumando todas las compras que su app tiene registradas en este telefono. Devuelve tambien cuantas compras son y hasta que evento del proveedor leyo la app.",
    "input_schema": { "type": "object", "properties": {}, "required": [] }
  },
  {
    "name": "examenes_revisar_mis_analitos",
    "title": "Revisar mis analitos",
    "description": "Revisa CADA analito del que la persona tiene historia y devuelve la tendencia de cada uno, marcando cuales exigen atencion. Sirve para preguntas generales como 'se me esta poniendo peor algo?'. No es la lista de examenes que la app ya muestra: es la LECTURA de cada serie en el tiempo, que ninguna pantalla da.",
    "input_schema": { "type": "object", "properties": {}, "required": [] }
  }
]
""";

    @Override
    public boolean onCreate() {
        Log.i(TAG, "provider: creado");
        return true;
    }

    @Override
    public Cursor query(Uri uri, String[] projection, String selection,
                        String[] selectionArgs, String sortOrder) {

        // QUIEN NOS PREGUNTA. El provider queda exportado sin permiso porque Claude no
        // declara tener ninguno; entonces la compuerta tiene que estar aca. Grado spike:
        // se registra, no se rechaza.
        Log.i(TAG, "provider: query " + uri
                + " · uid=" + Binder.getCallingUid()
                + " · pkg=" + getCallingPackage());

        MatrixCursor c = new MatrixCursor(new String[]{ "tools" });
        c.addRow(new Object[]{ TOOLS_JSON });
        return c;
    }

    @Override public String getType(Uri uri) { return "vnd.android.cursor.dir/vnd.com.ncubo.lab.octopus.tools"; }
    @Override public Uri insert(Uri uri, ContentValues values) { return null; }
    @Override public int delete(Uri uri, String sel, String[] args) { return 0; }
    @Override public int update(Uri uri, ContentValues v, String sel, String[] args) { return 0; }
}
