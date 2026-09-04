package com.ncubo.lab.caratula;

import android.app.Activity;
import android.app.appfunctions.AppFunctionManager;
import android.os.Bundle;
import android.os.OutcomeReceiver;
import android.util.Log;
import android.widget.TextView;

/**
 * EL AUTOTEST — la pregunta que decide el escalon A del spike.
 *
 * isAppFunctionEnabled tiene DOS sobrecargas: la de (functionId, targetPackage, ...) pide el
 * permiso EXECUTE_APP_FUNCTIONS, que en este aparato es internal|privileged y no se concede.
 * La de UN argumento pregunta por las funciones DE LA PROPIA APP, y no lo pide.
 *
 * Entonces esta Activity contesta, sin permisos de nadie: ¿el sistema operativo indexo la
 * funcion que declaramos en assets/app_function.xml?
 *
 *   true / false  -> SI la indexo (y ademas sabemos si esta habilitada)
 *   excepcion     -> NO la conoce
 */
public final class SelfTestActivity extends Activity {

    private static final String TAG = "SPIKE";
    private static final String FUNCTION_ID = "com.ncubo.lab.caratula#facturas_total_de_facturas";

    private TextView out;

    @Override protected void onCreate(Bundle saved) {
        super.onCreate(saved);
        out = new TextView(this);
        out.setPadding(40, 120, 40, 40);
        out.setTextSize(14f);
        setContentView(out);

        say("autotest de la caratula");
        say("function_id = " + FUNCTION_ID);

        AppFunctionManager mgr = getSystemService(AppFunctionManager.class);
        if (mgr == null) { say("AppFunctionManager: NULL"); return; }
        say("AppFunctionManager: obtenido");

        // Sobrecarga de UN argumento: mis propias funciones, sin permiso.
        mgr.isAppFunctionEnabled(FUNCTION_ID, getMainExecutor(),
            new OutcomeReceiver<Boolean, Exception>() {
                @Override public void onResult(Boolean enabled) {
                    say("INDEXADA por el SO · enabled=" + enabled);
                }
                @Override public void onError(Exception e) {
                    say("NO indexada · " + e.getClass().getSimpleName() + ": " + e.getMessage());
                }
            });
        say("consulta despachada, esperando…");
    }

    private void say(String s) {
        Log.i(TAG, "autotest: " + s);
        out.append(s + "\n\n");
    }
}
