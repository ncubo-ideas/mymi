package com.ncubo.gnothi.sonda;

import android.app.Activity;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.ServiceConnection;
import android.os.Bundle;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;
import android.os.Message;
import android.os.Messenger;
import android.os.RemoteException;
import android.util.Log;
import android.widget.TextView;

/**
 * LA SONDA — un IMPOSTOR, y ese es todo su valor.
 *
 * Hace exactamente lo mismo que la caratula legitima: liga al IpcService de Gnothi y le pide
 * una tool. La UNICA diferencia es quien es: otro paquete, otra firma, y nadie la declaro en
 * el installation.json de ningun modulo.
 *
 * POR QUE EXISTE. Una compuerta que deja pasar al que corresponde no esta probada: eso lo
 * logra tambien una compuerta que deja pasar a todo el mundo. Lo unico que distingue a las dos
 * es un llamado que TIENE que ser rechazado. Esta app es ese llamado.
 *
 * Se le pasan modulo y tool por extras para poder apuntarla a cualquier cosa desde adb:
 *
 *   am start -n com.ncubo.gnothi.sonda/.SondaActivity \
 *            --es modulo facturas --es tool total_de_facturas
 */
public final class SondaActivity extends Activity {

    private static final String TAG = "SPIKE";
    private static final String GNOTHI_PKG = "com.ncubo.gnothi.host";
    private static final String GNOTHI_SVC = "com.ncubo.gnothi.host.IpcService";

    private TextView out;

    @Override protected void onCreate(Bundle saved) {
        super.onCreate(saved);
        out = new TextView(this);
        out.setPadding(40, 120, 40, 40);
        out.setTextSize(14f);
        setContentView(out);

        final String modulo = getIntent().getStringExtra("modulo") != null
                ? getIntent().getStringExtra("modulo") : "facturas";
        final String tool = getIntent().getStringExtra("tool") != null
                ? getIntent().getStringExtra("tool") : "total_de_facturas";

        say("sonda: soy " + getPackageName());
        say("pido " + modulo + "." + tool + " a Gnothi");

        final long t0 = System.currentTimeMillis();
        final Context app = getApplicationContext();
        final ServiceConnection[] cell = new ServiceConnection[1];

        cell[0] = new ServiceConnection() {
            @Override public void onServiceConnected(ComponentName name, IBinder binder) {
                // LIGARSE SIEMPRE VA A FUNCIONAR: el service esta exportado, y tiene que
                // estarlo para que la caratula legitima pueda alcanzarlo. La compuerta no
                // vive en el bind, vive en la respuesta -- y esa distincion es justo lo que
                // esta prueba mide.
                say("ligado a los " + (System.currentTimeMillis() - t0) + " ms");
                try {
                    Messenger gnothi = new Messenger(binder);

                    Bundle data = new Bundle();
                    data.putString("module", modulo);
                    data.putString("tool", tool);

                    Message msg = Message.obtain();
                    msg.setData(data);
                    msg.replyTo = new Messenger(new Handler(Looper.getMainLooper()) {
                        @Override public void handleMessage(Message answer) {
                            Bundle d = answer.getData();
                            boolean ok = d != null && d.getBoolean("authorized", false);
                            String json = (d == null) ? "" : d.getString("json", "");
                            say((ok ? "PASE  " : "RECHAZO ") + "· " + json);
                            Log.i(TAG, "sonda: authorized=" + ok + " · " + json);
                            try { app.unbindService(cell[0]); } catch (Exception ignored) { }
                        }
                    });

                    gnothi.send(msg);
                } catch (RemoteException e) {
                    say("fallo el envio: " + e.getMessage());
                }
            }

            @Override public void onServiceDisconnected(ComponentName name) { }
        };

        Intent intent = new Intent();
        intent.setComponent(new ComponentName(GNOTHI_PKG, GNOTHI_SVC));
        boolean ok = app.bindService(intent, cell[0], Context.BIND_AUTO_CREATE);
        say("bindService=" + ok);
    }

    private void say(String s) {
        Log.i(TAG, "sonda: " + s);
        out.append(s + "\n\n");
    }
}
