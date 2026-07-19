{{- define "locatic.name" -}}
locatic
{{- end -}}

{{- define "locatic.labels" -}}
app.kubernetes.io/name: {{ include "locatic.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
helm.sh/chart: {{ .Chart.Name }}-{{ .Chart.Version | replace "+" "_" }}
{{- end -}}

{{- define "locatic.selectorApp" -}}
app.kubernetes.io/name: {{ include "locatic.name" . }}
app.kubernetes.io/component: app
{{- end -}}

{{- define "locatic.selectorNginx" -}}
app.kubernetes.io/name: {{ include "locatic.name" . }}
app.kubernetes.io/component: nginx
{{- end -}}
