package com.ncubo.lab.agente;

import android.app.Activity;
import android.app.appfunctions.AppFunctionException;
import android.app.appfunctions.AppFunctionManager;
import android.app.appfunctions.ExecuteAppFunctionRequest;
import android.app.appfunctions.ExecuteAppFunctionResponse;
import android.app.appsearch.GenericDocument;
import android.os.Bundle;
import android.os.CancellationSignal;
import android.os.OutcomeReceiver;
import android.util.Log;
import android.widget.TextView;

/**
 * UN AGENTE SIN PRIVILEGIOS — y esa falta de privilegios es todo el experimento.
 *
 * LA PREGUNTA. Alvaro escribio, y la referencia de Android lo confirma, que quienes descubren
 * e invocan AppFunctions son "agentes privilegiados por el sistema", y que una aplicacion
 * cualquiera no obtiene esa autoridad. De ahi su conclusion: lo unico que no se puede
 * demostrar es Gemini -> Gnothi en produccion.
 *
 * Pero en el framework de SDK 37 aparecio una perilla:
 *
 *     device_config  namespace app_function
 *                    clave     app_function_additional_agent_allowlist
 *     y el metodo    verifyCallerCanExecuteAppFunctionWithAllowlist
 *
 * «Allowlist de agentes ADICIONALES». Esta app existe para contestar si esa perilla
 * realmente deja entrar a un tercero.
 *
 * EN QUE SE DIFERENCIA DEL AGENTE DE PRUEBA DE GOOGLE. El de Google se lanza con
 * `am instrument`, que le hace ADOPTAR LA IDENTIDAD DE SHELL — y shell ya tiene el permiso.
 * Por eso su corrida no prueba nada sobre terceros. Esta app se lanza normal, como cualquier
 * app que instalaria una persona.
 *
 * COMO SE LEE EL RESULTADO — y las dos corridas importan:
 *
 *   SIN la perilla puesta   se espera  SecurityException / ERROR_DENIED
 *   CON la perilla puesta   si PASA -> la frontera del EAP se corre para desarrollo
 *                           si NO   -> la allowlist es de otra cosa, y queda cerrado
 *
 * Una compuerta que rechaza a todos no se distingue de una que funciona si solo se prueba
 * el caso que debe fallar. Por eso hay que correr las dos.
 */
public final class AgenteActivity extends Activity {

    private static final String TAG = "SPIKE";

    /** La caratula de Gnothi y su funcion, tal como quedaron declaradas en el indice del SO. */
    private static final String OBJETIVO = "com.ncubo.lab.caratula";
    private static final String FUNCION  = "com.ncubo.lab.caratula#facturas_total_de_facturas";

    private TextView out;

    @Override protected void onCreate(Bundle saved) {
        super.onCreate(saved);
        out = new TextView(this);
        out.setPadding(40, 120, 40, 40);
        out.setTextSize(13f);
        setContentView(out);

        say("agente: soy " + getPackageName() + ", sin privilegios de sistema");
        say("objetivo: " + FUNCION);

        AppFunctionManager mgr = getSystemService(AppFunctionManager.class);
        if (mgr == null) {
            say("RESULTADO: AppFunctionManager es NULL — el servicio no esta expuesto a esta app");
            return;
        }
        say("AppFunctionManager: obtenido");

        final long t0 = System.currentTimeMillis();

        try {
            ExecuteAppFunctionRequest req =
                    new ExecuteAppFunctionRequest.Builder(OBJETIVO, FUNCION).build();

            mgr.executeAppFunction(req, getMainExecutor(), new CancellationSignal(),
                new OutcomeReceiver<ExecuteAppFunctionResponse, AppFunctionException>() {

                    @Override public void onResult(ExecuteAppFunctionResponse r) {
                        // Si esto corre, la perilla dejo entrar a un tercero.
                        GenericDocument d = r.getResultDocument();
                        String valor = (d == null) ? "(sin documento)"
                                : d.getPropertyString(ExecuteAppFunctionResponse.PROPERTY_RETURN_VALUE);
                        say("PASO · a los " + (System.currentTimeMillis() - t0) + " ms");
                        say(String.valueOf(valor));
                    }

                    @Override public void onError(AppFunctionException e) {
                        // El codigo importa mas que el mensaje: distingue "no tenes permiso"
                        // de "no existe la funcion", que es un fallo de otra cosa.
                        say("RECHAZO · codigo=" + e.getErrorCode()
                                + " · " + e.getErrorMessage());
                    }
                });

            say("pedido despachado, esperando…");

        } catch (SecurityException e) {
            // El camino mas probable SIN la perilla: ni siquiera llega a despacharse.
            say("SecurityException al despachar: " + e.getMessage());
        } catch (Exception e) {
            say(e.getClass().getSimpleName() + ": " + e.getMessage());
        }
    }

    private void say(String s) {
        Log.i(TAG, "agente: " + s);
        out.append(s + "\n\n");
    }
}
